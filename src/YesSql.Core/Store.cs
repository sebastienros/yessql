using Dapper;
using Roslyn.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
        protected LinearBlockIdGenerator IdGenerator;
        private ObjectPool<Session> _sessionPool;

        public IConfiguration Configuration { get; set; }

        internal readonly ConcurrentDictionary<Type, Func<IIndex, object>> GroupMethods =
            new ConcurrentDictionary<Type, Func<IIndex, object>>();

        internal readonly ConcurrentDictionary<TypeCollectionTuple, IEnumerable<IndexDescriptor>> Descriptors =
            new ConcurrentDictionary<TypeCollectionTuple, IEnumerable<IndexDescriptor>>();

        internal readonly ConcurrentDictionary<Type, IIdAccessor<int>> _idAccessors =
            new ConcurrentDictionary<Type, IIdAccessor<int>>();

        internal readonly ConcurrentDictionary<Type, Func<IDescriptor>> DescriptorActivators =
            new ConcurrentDictionary<Type, Func<IDescriptor>>();

        internal readonly ConcurrentDictionary<WorkerQueryKey, Task<object>> Workers =
            new ConcurrentDictionary<WorkerQueryKey, Task<object>>();

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
        public Store(Action<IConfiguration> config)
        {
            Configuration = new Configuration();
            config?.Invoke(Configuration);

            AfterConfigurationAssigned();
        }

        /// <summary>
        /// Initializes a <see cref="Store"/> instance using a specific <see cref="Configuration"/> instance.
        /// </summary>
        /// <param name="configuration">The <see cref="Configuration"/> instance to use.</param>
        public Store(IConfiguration configuration)
        {
            Configuration = configuration;

            AfterConfigurationAssigned();
        }

        public void AfterConfigurationAssigned()
        {
            IndexCommand.ResetQueryCache();
            Indexes = new List<IIndexProvider>();
            ValidateConfiguration();
            IdGenerator = new LinearBlockIdGenerator(Configuration.ConnectionFactory, Configuration.LinearBlockSize, Configuration.TablePrefix);

            _sessionPool = new ObjectPool<Session>(MakeSession, Configuration.SessionPoolSize);
        }

        public Task InitializeAsync()
        {
            using (var session = CreateSession())
            {
                var builder = new SchemaBuilder(session);

                builder.CreateTable("Document", table => table
                    .Column<int>("Id", column => column.PrimaryKey().NotNull())
                    .Column<string>("Type", column => column.NotNull())
                    .Column<string>("Content", column => column.Unlimited())
                )
                .AlterTable("Document", table => table
                    .CreateIndex("IX_Type", "Type")
                );

                builder.CreateTable(LinearBlockIdGenerator.TableName, table => table
                    .Column<string>("dimension", column => column.PrimaryKey().NotNull())
                    .Column<ulong>("nextval")
                )
                .AlterTable(LinearBlockIdGenerator.TableName, table => table
                    .CreateIndex("IX_Dimension", "dimension")
                );
            }

#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        public Task InitializeCollectionAsync(string collectionName)
        {
            var documentTable = collectionName + "_" + "Document";

            using (var session = CreateSession())
            {
                var builder = new SchemaBuilder(session);

                builder
                    .CreateTable(documentTable, table => table
                    .Column<int>("Id", column => column.PrimaryKey().NotNull())
                    .Column<string>("Type", column => column.NotNull())
                    .Column<string>("Content", column => column.Unlimited())
                )
                .AlterTable(documentTable, table => table
                    .CreateIndex("IX_" + documentTable + "_Type", "Type")
                );
            }

#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
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
            return _idAccessors.GetOrAdd(tContainer, type => Configuration.IdentifierFactory.CreateAccessor<int>(tContainer, name));
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

            var tupe = new TypeCollectionTuple(target, collection);

            return Descriptors.GetOrAdd(tupe, key =>
            {
                var activator = DescriptorActivators.GetOrAdd(key.Type, type => MakeDescriptorActivator(type));
                var context = activator();

                foreach (var provider in Indexes)
                {
                    if (provider.ForType().IsAssignableFrom(target) &&
                        String.Equals(key.Collection, provider.CollectionName, StringComparison.OrdinalIgnoreCase))
                    {
                        provider.Describe(context);
                    }
                }

                return context.Describe(new[] { target }).ToList();
            });
        }

        private static Func<IDescriptor> MakeDescriptorActivator(Type type)
        {
            var contextType = typeof(DescribeContext<>).MakeGenericType(type);
            return Expression.Lambda<Func<IDescriptor>>(Expression.New(contextType)).Compile();
        }

        public int GetNextId(ISession session, string collection)
        {
            return (int)IdGenerator.GetNextId(session, collection);
        }

        public IStore RegisterIndexes(params IIndexProvider[] indexProviders)
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

        /// <summary>
        /// Enlists some reusable logic such that not two threads run the same thing.
        /// </summary>
        /// <param name="key">A key identifying the running work.</param>
        /// <param name="work">A function containing the logic to execute.</param>
        /// <returns>The result of the work.</returns>
        public async Task<T> ProduceAsync<T>(WorkerQueryKey key, Func<Task<T>> work)
        {
            if (!Configuration.QueryGatingEnabled)
            {
                return await work();
            }

            object content = null;

            while (content == null)
            {
                // Is there any query already processing the ?
                if (!Workers.TryGetValue(key, out var result))
                {
                    // Multiple threads can potentially reach this point which is fine
                    var tcs = new TaskCompletionSource<object>();

                    Workers.TryAdd(key, tcs.Task);

                    try
                    {
                        // The current worker is processed
                        content = await work();
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

            return (T) content;
        }

        internal class TypeCollectionTuple : Tuple<Type, string>
        {
            public TypeCollectionTuple(Type type, string collection) : base(type, collection)
            {
            }

            public Type Type { get { return Item1; } }
            public string Collection { get { return Item2; } }
        }
    }
}
