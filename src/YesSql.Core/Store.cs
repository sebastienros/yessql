using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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

        public IConfiguration Configuration { get; set; }
        public ISqlDialect Dialect { get; private set; }
        public ITypeService TypeNames { get; private set; }

        internal ImmutableDictionary<Type, Func<IIndex, object>> GroupMethods =
            ImmutableDictionary<Type, Func<IIndex, object>>.Empty;

        internal ImmutableDictionary<string, IEnumerable<IndexDescriptor>> Descriptors =
            ImmutableDictionary<string, IEnumerable<IndexDescriptor>>.Empty;

        internal ImmutableDictionary<Type, IAccessor<int>> IdAccessors =
            ImmutableDictionary<Type, IAccessor<int>>.Empty;

        internal ImmutableDictionary<Type, IAccessor<int>> VersionAccessors =
            ImmutableDictionary<Type, IAccessor<int>>.Empty;

        internal ImmutableDictionary<Type, Func<IDescriptor>> DescriptorActivators =
            ImmutableDictionary<Type, Func<IDescriptor>>.Empty;

        internal readonly ConcurrentDictionary<WorkerQueryKey, Task<object>> Workers =
            new ConcurrentDictionary<WorkerQueryKey, Task<object>>();

        internal ImmutableDictionary<long, QueryState> CompiledQueries =
            ImmutableDictionary<long, QueryState>.Empty;

        internal const int SmallBufferSize = 128;
        internal const int MediumBufferSize = 512;
        internal const int LargeBufferSize = 1024;

        static Store()
        {
            SqlMapper.ResetTypeHandlers();

            // Databases that don't support DateTimeOffset natively will store these in string columns.
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            
            // Required by Sqlite. Guids are stored as text (uniqueidentifier) and are converted back to Guid with this handler.
            SqlMapper.AddTypeHandler(new GuidHandler());

            // Databases that don't support TimeSpan natively will store these in int columns as ticks.
            SqlMapper.AddTypeHandler(new TimeSpanHandler());
        }

        private Store()
        {
            Indexes = new List<IIndexProvider>();
            ScopedIndexes = new List<Type>();
        }

        /// <summary>
        /// Initializes a <see cref="Store"/> instance and its new <see cref="Configuration"/>.
        /// </summary>
        /// <param name="config">An action to execute on the <see cref="Configuration"/> of the new <see cref="Store"/> instance.</param>
        internal Store(Action<IConfiguration> config) : this()
        {
            Configuration = new Configuration();
            config?.Invoke(Configuration);
            Dialect = Configuration.SqlDialect;
        }

        /// <summary>
        /// Initializes a <see cref="Store"/> instance using a specific <see cref="Configuration"/> instance.
        /// </summary>
        /// <param name="configuration">The <see cref="Configuration"/> instance to use.</param>
        internal Store(IConfiguration configuration) : this()
        {
            Configuration = configuration;
            Dialect = Configuration.SqlDialect;
        }

        public async Task InitializeAsync()
        {
            IndexCommand.ResetQueryCache();
            ValidateConfiguration();

            TypeNames = new TypeService();

#if SUPPORTS_ASYNC_TRANSACTIONS
            await using (var connection = Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using (var transaction = connection.BeginTransaction(Configuration.IsolationLevel))
#else
            using (var connection = Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(Configuration.IsolationLevel))
#endif            
                {
                    var builder = new SchemaBuilder(Configuration, transaction);
                    await Configuration.IdGenerator.InitializeAsync(this, builder);

#if SUPPORTS_ASYNC_TRANSACTIONS
                    await transaction.CommitAsync();
#else
                    transaction.Commit();
#endif
                }
            }

            // Pre-initialize the default collection
            await InitializeCollectionAsync("");
        }

        public async Task InitializeCollectionAsync(string collection)
        {
            var documentTable = Configuration.TableNameConvention.GetDocumentTable(collection);

#if SUPPORTS_ASYNC_TRANSACTIONS
            await using (var connection = Configuration.ConnectionFactory.CreateConnection())
#else
            using (var connection = Configuration.ConnectionFactory.CreateConnection())
#endif            
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
#if SUPPORTS_ASYNC_TRANSACTIONS
                                await result.CloseAsync();
#else
                                result.Close();
#endif
                                using (var migrationTransaction = connection.BeginTransaction())
                                {
                                    var migrationBuilder = new SchemaBuilder(Configuration, migrationTransaction);

                                    try
                                    {
                                        migrationBuilder
                                            .AlterTable(documentTable, table => table
                                                .AddColumn<long>(nameof(Document.Version), column => column.WithDefault(0))
                                            );

#if SUPPORTS_ASYNC_TRANSACTIONS
                                        await migrationTransaction.CommitAsync();
#else
                                        migrationTransaction.Commit();
#endif
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
#if SUPPORTS_ASYNC_TRANSACTIONS
                    await using (var transaction = connection.BeginTransaction())
#else
                    using (var transaction = connection.BeginTransaction())
#endif
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
                                .Column<long>(nameof(Document.Version), column => column.NotNull().WithDefault(0))
                            )
                            .AlterTable(documentTable, table => table
                                .CreateIndex("IX_" + documentTable + "_Type", "Type")
                            );

#if SUPPORTS_ASYNC_TRANSACTIONS
                            await transaction.CommitAsync();
#else
                            transaction.Commit();
#endif
                        }
                        catch
                        {
                            // Another thread must have created it
                        }
                    }
                }
                finally
                {
                    await Configuration.IdGenerator.InitializeCollectionAsync(Configuration, collection);
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
            return new Session(this);
        }

        public void Dispose()
        {
        }

        public IAccessor<int> GetIdAccessor(Type tContainer)
        {
            if (!IdAccessors.TryGetValue(tContainer, out var result))
            {
                result = Configuration.IdentifierAccessorFactory.CreateAccessor<int>(tContainer);

                IdAccessors = IdAccessors.SetItem(tContainer, result);
            }

            return result;
        }

        public IAccessor<int> GetVersionAccessor(Type tContainer)
        {
            if (!VersionAccessors.TryGetValue(tContainer, out var result))
            {
                result = Configuration.VersionAccessorFactory.CreateAccessor<int>(tContainer);

                VersionAccessors = VersionAccessors.SetItem(tContainer, result);
            }

            return result;
        }

        /// <summary>
        /// Returns the available indexers for a specified type
        /// </summary>
        public IEnumerable<IndexDescriptor> Describe(Type target, string collection)
        {
            if (target == null)
            {
                throw new ArgumentNullException();
            }

            var cacheKey = String.IsNullOrEmpty(collection)
                ? target.FullName
                : target.FullName + ":" + collection
                ;

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

        public IStore RegisterIndexes(IEnumerable<IIndexProvider> indexProviders, string collection = null)
        {
            foreach (var indexProvider in indexProviders)
            {
                if (indexProvider.CollectionName == null)
                {
                    indexProvider.CollectionName = collection ?? "";
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
        internal async Task<T> ProduceAsync<T, TState>(WorkerQueryKey key, Func<TState, Task<T>> work, TState state)
        {
            if (!Configuration.QueryGatingEnabled)
            {
                return await work(state);
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
                        content = await work(state);
                    }
                    catch
                    {
                        // An exception occurred in the main worker, we broadcast the null value
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
