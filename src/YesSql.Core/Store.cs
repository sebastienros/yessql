using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        internal readonly ConcurrentDictionary<Type, Func<IIndex, object>> GroupMethods = new();

        internal readonly ConcurrentDictionary<string, IEnumerable<IndexDescriptor>> Descriptors = new();

        internal readonly ConcurrentDictionary<Type, IAccessor<long>> IdAccessors = new();

        internal readonly ConcurrentDictionary<Type, IAccessor<long>> VersionAccessors = new();

        internal readonly ConcurrentDictionary<Type, Func<IDescriptor>> DescriptorActivators = new();

        internal readonly ConcurrentDictionary<WorkerQueryKey, Task<object>> Workers = new();

        internal readonly ConcurrentDictionary<long, QueryState> CompiledQueries = new();

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

            if (!string.IsNullOrEmpty(Configuration.Schema))
            {
                await using (var connection = Configuration.ConnectionFactory.CreateConnection())
                {
                    await connection.OpenAsync();

                    await using var transaction = await connection.BeginTransactionAsync(Configuration.IsolationLevel);

                    var builder = new SchemaBuilder(Configuration, transaction);

                    await builder.CreateSchemaAsync(Configuration.Schema);

                    await transaction.CommitAsync();
                }
            }

            // Initialize the Id generator
            await Configuration.IdGenerator.InitializeAsync(this);

            // Pre-initialize the default collection
            await InitializeCollectionAsync(string.Empty);
        }

        public async Task InitializeCollectionAsync(string collection)
        {
            var documentTable = Configuration.TableNameConvention.GetDocumentTable(collection);

            await using var connection = Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            try
            {
                var selectCommand = connection.CreateCommand();

                var selectBuilder = Dialect.CreateBuilder(Configuration.TablePrefix);
                selectBuilder.Select();
                selectBuilder.AddSelector("*");
                selectBuilder.Table(documentTable, null, Configuration.Schema);
                selectBuilder.Take("1");

                selectCommand.CommandText = selectBuilder.ToSqlString();

                Configuration.Logger.LogTrace(selectCommand.CommandText);

                using var result = await selectCommand.ExecuteReaderAsync();
                if (result != null)
                {
                    try
                    {
                        // Check if the Version column exists
                        result.GetOrdinal(nameof(Document.Version));
                    }
                    catch
                    {
                        await result.CloseAsync();
                        await using var migrationTransaction = await connection.BeginTransactionAsync();
                        var migrationBuilder = new SchemaBuilder(Configuration, migrationTransaction);

                        try
                        {
                            await migrationBuilder.AlterTableAsync(documentTable, table => table
                                    .AddColumn<long>(nameof(Document.Version), column => column.WithDefault(0))
                                );

                            await migrationTransaction.CommitAsync();
                        }
                        catch
                        {
                            // Another thread must have altered it
                            await migrationTransaction.RollbackAsync();
                        }
                    }
                    return;
                }
            }
            catch
            {
                await using var transaction = await connection.BeginTransactionAsync();
                var builder = new SchemaBuilder(Configuration, transaction);

                try
                {
                    // The table doesn't exist, create it
                    await builder.CreateTableAsync(documentTable, table => table
                        .Column(Configuration.IdentityColumnSize, nameof(Document.Id), column => column.PrimaryKey().NotNull())
                        .Column<string>(nameof(Document.Type), column => column.NotNull())
                        .Column<string>(nameof(Document.Content), column => column.Unlimited())
                        .Column<long>(nameof(Document.Version), column => column.NotNull().WithDefault(0))
                    );

                    await builder.AlterTableAsync(documentTable, table => table
                        .CreateIndex("IX_" + documentTable + "_Type", "Type")
                    );

                    await transaction.CommitAsync();
                }
                catch
                {
                    // Another thread must have created it
                }
            }
            finally
            {
                await Configuration.IdGenerator.InitializeCollectionAsync(Configuration, collection);
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
            => new Session(this);

        public void Dispose()
        {
        }

        public IAccessor<long> GetIdAccessor(Type tContainer)
        => IdAccessors.GetOrAdd(tContainer, Configuration.IdentifierAccessorFactory.CreateAccessor<long>);

        public IAccessor<long> GetVersionAccessor(Type tContainer)
            => VersionAccessors.GetOrAdd(tContainer, Configuration.VersionAccessorFactory.CreateAccessor<long>);

        /// <summary>
        /// Returns the available indexers for a specified type
        /// </summary>
        public IEnumerable<IndexDescriptor> Describe(Type target, string collection)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var cacheKey = string.IsNullOrEmpty(collection)
                ? target.FullName
                : target.FullName + ":" + collection
                ;

            return Descriptors.GetOrAdd(cacheKey, key => CreateDescriptors(target, collection, Indexes));
        }

        internal IEnumerable<IndexDescriptor> CreateDescriptors(Type target, string collection, IEnumerable<IIndexProvider> indexProviders)
        {
            var activator = DescriptorActivators.GetOrAdd(target, MakeDescriptorActivator);

            var context = activator();

            foreach (var provider in indexProviders)
            {
                if (provider.ForType().IsAssignableFrom(target) &&
                    string.Equals(collection, provider.CollectionName, StringComparison.OrdinalIgnoreCase))
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

        [Obsolete($"Instead, utilize the {nameof(GetNextIdAsync)} method. This current method is slated for removal in upcoming releases.")]
        public long GetNextId(string collection)
            => GetNextIdAsync(collection).ConfigureAwait(false).GetAwaiter().GetResult();

        public Task<long> GetNextIdAsync(string collection)
            => Configuration.IdGenerator.GetNextIdAsync(collection);

        public IStore RegisterIndexes(IEnumerable<IIndexProvider> indexProviders, string collection = null)
        {
            foreach (var indexProvider in indexProviders)
            {
                indexProvider.CollectionName ??= collection ?? "";
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
                    // c.f. https://blogs.msdn.microsoft.com/seteplia/2018/10/01/the-danger-of-taskcompletionsourcet-class/
                    var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                        Workers.TryRemove(key, out _);

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
