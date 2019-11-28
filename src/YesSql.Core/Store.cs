using Dapper;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Commands;
using YesSql.Data;
using YesSql.Indexes;
using YesSql.Services;
using YesSql.Sql;

namespace YesSql
{
    public class Store : IStore
    {
        protected List<IIndexProvider> Indexes;
        protected List<Type> ScopedIndexes;

        private ObjectPool<Session> _sessionPool;

        public IConfiguration Configuration { get; set; }
        public ISqlDialect Dialect { get; private set; }
        public ITypeService TypeNames { get; private set; }

        internal ImmutableDictionary<Type, Func<IIndex, object>> GroupMethods =
            ImmutableDictionary<Type, Func<IIndex, object>>.Empty;

        internal ImmutableDictionary<string, IEnumerable<IndexDescriptor>> Descriptors =
            ImmutableDictionary<string, IEnumerable<IndexDescriptor>>.Empty;

        internal ImmutableDictionary<Type, IIdAccessor<int>> IdAccessors =
            ImmutableDictionary<Type, IIdAccessor<int>>.Empty;

        internal ImmutableDictionary<Type, Func<IDescriptor>> DescriptorActivators =
            ImmutableDictionary<Type, Func<IDescriptor>>.Empty;

        internal readonly ConcurrentDictionary<WorkerQueryKey, Task<object>> Workers =
            new ConcurrentDictionary<WorkerQueryKey, Task<object>>();

        internal ImmutableDictionary<Type, QueryState> CompiledQueries =
            ImmutableDictionary<Type, QueryState>.Empty;

        public const string DocumentTable = "Document";

        static Store()
        {
            SqlMapper.ResetTypeHandlers();

            // Add Type Handlers here
        }

        /// <summary>
        /// Initializes a <see cref="Store"/> instance and its new <see cref="Configuration"/>.
        /// </summary>
        /// <param name="config">An action to execute on the <see cref="Configuration"/> of the new <see cref="Store"/> instance.</param>
        internal Store(Action<IConfiguration> config)
        {
            Configuration = new Configuration();
            config?.Invoke(Configuration);
        }

        /// <summary>
        /// Initializes a <see cref="Store"/> instance using a specific <see cref="Configuration"/> instance.
        /// </summary>
        /// <param name="configuration">The <see cref="Configuration"/> instance to use.</param>
        internal Store(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        internal async Task InitializeAsync()
        {
            IndexCommand.ResetQueryCache();
            Indexes = new List<IIndexProvider>();
            ScopedIndexes = new List<Type>();
            ValidateConfiguration();

            _sessionPool = new ObjectPool<Session>(MakeSession, Configuration.SessionPoolSize);
            Dialect = SqlDialectFactory.For(Configuration.ConnectionFactory.DbConnectionType);
            TypeNames = new TypeService();

            using (var connection = Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(Configuration, transaction);
                    await Configuration.IdGenerator.InitializeAsync(this, builder);

                    transaction.Commit();
                }
            }

            // Pee-initialize the default collection
            await InitializeCollectionAsync("");
        }

        public async Task InitializeCollectionAsync(string collectionName)
        {
            var documentTable = String.IsNullOrEmpty(collectionName) ? "Document" : collectionName + "_" + "Document";

            using (var connection = Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                try
                {
                    var selectCommand = connection.CreateCommand();

                    var selectBuilder = Dialect.CreateBuilder(Configuration.TablePrefix);
                    selectBuilder.Select();
                    selectBuilder.AddSelector("*");
                    selectBuilder.Table(documentTable);
                    selectBuilder.Take("1");

                    selectCommand.CommandText = selectBuilder.ToSqlString();
                    Configuration.Logger.LogTrace(selectCommand.CommandText);

                    using (var result = await selectCommand.ExecuteReaderAsync())
                    {
                        if (result != null)
                        {
                            try
                            {
                                // Check if the Version column exists
                                result.GetOrdinal(nameof(Document.Version));
                            }
                            catch
                            {
                                result.Close();
                                using (var migrationTransaction = connection.BeginTransaction())
                                {
                                    var migrationBuilder = new SchemaBuilder(Configuration, migrationTransaction);

                                    try
                                    {
                                        migrationBuilder
                                            .AlterTable(documentTable, table => table
                                                .AddColumn<long>(nameof(Document.Version), column => column.WithDefault(0))
                                            );

                                        migrationTransaction.Commit();
                                    }
                                    catch
                                    {

                                        // Another thread must have altered it
                                    }
                                }
                            }
                            return;
                        }
                    }
                }
                catch
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        var builder = new SchemaBuilder(Configuration, transaction);

                        try
                        {
                            // The table doesn't exist, create it
                            builder
                                .CreateTable(documentTable, table => table
                                .Column<int>(nameof(Document.Id), column => column.PrimaryKey().NotNull())
                                .Column<string>(nameof(Document.Type), column => column.NotNull())
                                .Column<string>(nameof(Document.Content), column => column.Unlimited())
                                .Column<long>(nameof(Document.Version), column => column.WithDefault(0))
                            )
                            .AlterTable(documentTable, table => table
                                .CreateIndex("IX_" + documentTable + "_Type", "Type")
                            );

                            transaction.Commit();
                        }
                        catch
                        {
                            // Another thread must have created it
                        }
                    }
                }
                finally
                {
                    await Configuration.IdGenerator.InitializeCollectionAsync(Configuration, collectionName);
                }
            }
        }

        private void ValidateConfiguration()
        {
            if (Configuration.ConnectionFactory == null)
            {
                throw new Exception("The connection factory should be initialized during configuration.");
            }
        }

        public ISession CreateSession()
        {
            return CreateSession(Configuration.IsolationLevel);
        }

        public ISession CreateSession(IsolationLevel isolationLevel)
        {
            var session = _sessionPool.Allocate();
            session.StartLease(isolationLevel);
            return session;
        }

        /// <summary>
        /// Called by the Session pool to make a new instance.
        /// </summary>
        /// <returns></returns>
        private Session MakeSession()
        {
            return new Session(this, Configuration.IsolationLevel);
        }

        internal void ReleaseSession(Session session)
        {
            _sessionPool.Free(session);
        }

        public void Dispose()
        {
        }

        public IIdAccessor<int> GetIdAccessor(Type tContainer, string name)
        {
            if (!IdAccessors.TryGetValue(tContainer, out var result))
            {
                result = Configuration.IdentifierFactory.CreateAccessor<int>(tContainer, name);

                // Don't use Add as two thread could concurrently reach this point.
                // We don't mind losing some values as the next call will restore it if it's not cached.
                IdAccessors = IdAccessors.SetItem(tContainer, result);
            }

            return result;
        }

        /// <summary>
        /// Returns the available indexers for a specified type
        /// </summary>
        public IEnumerable<IndexDescriptor> Describe(Type target)
        {
            if (target == null)
            {
                throw new ArgumentNullException();
            }

            var collection = CollectionHelper.Current.GetSafeName();
            var cacheKey = target.FullName + ":" + collection;

            if (!Descriptors.TryGetValue(cacheKey, out var result))
            {
                result = CreateDescriptors(target, collection, Indexes);

                // Don't use Add as two thread could concurrently reach this point.
                // We don't mind losing some values as the next call will restore it if it's not cached.
                Descriptors = Descriptors.SetItem(cacheKey, result);
            }

            return result;
        }

        internal IEnumerable<IndexDescriptor> CreateDescriptors(Type target, string collection, IEnumerable<IIndexProvider> indexProviders)
        {
            if (!DescriptorActivators.TryGetValue(target, out var activator))
            {
                activator = MakeDescriptorActivator(target);

                // Don't use Add as two thread could concurrently reach this point.
                // We don't mind losing some values as the next call will restore it if it's not cached.
                DescriptorActivators = DescriptorActivators.SetItem(target, activator);
            }

            var context = activator();

            foreach (var provider in indexProviders)
            {
                if (provider.ForType().IsAssignableFrom(target) &&
                    String.Equals(collection, provider.CollectionName, StringComparison.OrdinalIgnoreCase))
                {
                    provider.Describe(context);
                }
            }

            return context.Describe(new[] { target }).ToList();
        }

        private static Func<IDescriptor> MakeDescriptorActivator(Type type)
        {
            var contextType = typeof(DescribeContext<>).MakeGenericType(type);
            return Expression.Lambda<Func<IDescriptor>>(Expression.New(contextType)).Compile();
        }

        public int GetNextId(string collection)
        {
            return (int)Configuration.IdGenerator.GetNextId(collection);
        }

        public IStore RegisterIndexes(IEnumerable<IIndexProvider> indexProviders)
        {
            foreach (var indexProvider in indexProviders)
            {
                if (indexProvider.CollectionName == null)
                {
                    indexProvider.CollectionName = CollectionHelper.Current.GetSafeName();
                }
            }

            Indexes.AddRange(indexProviders);
            return this;
        }

        public IStore RegisterScopedIndexes(IEnumerable<Type> indexProviders)
        {
            ScopedIndexes.AddRange(indexProviders);
            return this;
        }

        /// <summary>
        /// Enlists some reusable logic such that not two threads run the same thing.
        /// </summary>
        /// <param name="key">A key identifying the running work.</param>
        /// <param name="work">A function containing the logic to execute.</param>
        /// <returns>The result of the work.</returns>
        internal async Task<T> ProduceAsync<T>(WorkerQueryKey key, Func<object[], Task<T>> work, params object[] args)
        {
            if (!Configuration.QueryGatingEnabled)
            {
                return await work(args);
            }

            object content = null;

            while (content == null)
            {
                // Is there any query already processing the ?
                if (!Workers.TryGetValue(key, out var result))
                {
                    // Multiple threads can potentially reach this point which is fine
#if !NET451
                    // c.f. https://blogs.msdn.microsoft.com/seteplia/2018/10/01/the-danger-of-taskcompletionsourcet-class/
                    var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
#else
                    var tcs = new TaskCompletionSource<object>();
#endif

                    Workers.TryAdd(key, tcs.Task);

                    try
                    {
                        // The current worker is processed
                        content = await work(args);
                    }
                    catch
                    {
                        // An exception occured in the main worker, we broadcast the null value
                        content = null;
                        throw;
                    }
                    finally
                    {
                        // Remove the worker task before setting the result.
                        // If the result is null, other threads would potentially
                        // acquire it otherwise.
                        Workers.TryRemove(key, out result);

                        // Notify all other awaiters to return the result
                        tcs.TrySetResult(content);
                    }
                }
                else
                {
                    // Another worker is already running, wait for it to finish and reuse the results.
                    // This value can be null if the worker failed, in this case the loop will run again.
                    content = await result;
                }
            }

            return (T)content;
        }
    }
}
