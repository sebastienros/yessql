using Dapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Commands;
using YesSql.Indexes;
using YesSql.Services;
using YesSql.Sql;

namespace YesSql
{
    public class Store : IStore
    {
        protected List<IIndexProvider> Indexes;
        protected LinearBlockIdGenerator IdGenerator;

        public IConfiguration Configuration { get; set; }

        internal readonly ConcurrentDictionary<Type, Func<IIndex, object>> GroupMethods =
            new ConcurrentDictionary<Type, Func<IIndex, object>>();

        internal readonly ConcurrentDictionary<TypeCollectionTuple, IEnumerable<IndexDescriptor>> Descriptors =
            new ConcurrentDictionary<TypeCollectionTuple, IEnumerable<IndexDescriptor>>();

        internal readonly ConcurrentDictionary<Type, IIdAccessor<int>> _idAccessors =
            new ConcurrentDictionary<Type, IIdAccessor<int>>();

        internal readonly ConcurrentDictionary<Type, Func<IDescriptor>> DescriptorActivators =
            new ConcurrentDictionary<Type, Func<IDescriptor>>();

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
            IdGenerator = new LinearBlockIdGenerator(Configuration.ConnectionFactory, 20, Configuration.TablePrefix);
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
            return new Session(this, Configuration.IsolationLevel);
        }

        public ISession CreateSession(IsolationLevel isolationLevel)
        {
            return new Session(this, isolationLevel);
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

            // TODO: Implement a more performant activator
            return () => Activator.CreateInstance(contextType) as IDescriptor;
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
