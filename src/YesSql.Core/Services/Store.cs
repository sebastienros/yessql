using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YesSql.Core.Data;
using YesSql.Core.Indexes;

namespace YesSql.Core.Services
{
    public class Store : IStore
    {
        protected readonly IList<IIndexProvider> Indexes;
        protected readonly LinearBlockIdGenerator IdGenerator;

        public Configuration Configuration
        {
            get; set;
        }

        internal readonly ConcurrentDictionary<Type, Func<IIndex, object>> GroupMethods =
            new ConcurrentDictionary<Type, Func<IIndex, object>>();

        internal readonly ConcurrentDictionary<Type, IEnumerable<IndexDescriptor>> Descriptors =
            new ConcurrentDictionary<Type, IEnumerable<IndexDescriptor>>();

        internal readonly ConcurrentDictionary<Type, IIdAccessor<int>> _idAccessors =
            new ConcurrentDictionary<Type, IIdAccessor<int>>();


        public Store(Configuration configuration)
        {
            Configuration = configuration;
            Indexes = new List<IIndexProvider>();
            ValidateConfiguration();
            IdGenerator = new LinearBlockIdGenerator(Configuration.ConnectionFactory, 20, "index", Configuration.TablePrefix);
        }

        public async Task InitializeAsync()
        {
            using (var session = CreateSession())
            {
                session.ExecuteMigration(builder =>
                {
                    builder
                        .CreateTable("Document", table => table
                        .Column<int>("Id", column => column.PrimaryKey().NotNull())
                        .Column<string>("Type", column => column.NotNull())
                    )
                    .AlterTable("Document", table => table
                        .CreateIndex("IX_Type", "Type")
                    );

                    builder.CreateTable(LinearBlockIdGenerator.TableName, table => table
                        .Column<string>("dimension")
                        .Column<ulong>("nextval")
                    );
                });
            }

            await Configuration.DocumentStorageFactory.InitializeAsync(Configuration);
        }

        private void ValidateConfiguration()
        {
            if (Configuration.ConnectionFactory == null)
            {
                throw new Exception("The connection factory should be initialized during configuration.");
            }

            if (Configuration.DocumentStorageFactory == null)
            {
                throw new Exception("The document storage factory should be initialized during configuration.");
            }
        }

        public ISession CreateSession()
        {
            return new Session(s => Configuration.DocumentStorageFactory.CreateDocumentStorage(s, Configuration), this, Configuration.IsolationLevel);
        }

        public ISession CreateSession(IsolationLevel isolationLevel)
        {
            return new Session(s => Configuration.DocumentStorageFactory.CreateDocumentStorage(s, Configuration), this, isolationLevel);
        }

        public void Dispose()
        {
            var disposableFactory = Configuration.DocumentStorageFactory as IDisposable;
            if (disposableFactory != null)
            {
                disposableFactory.Dispose();
            }
        }

        public IStore RegisterIndexes<T>() where T : IIndexProvider
        {
            return RegisterIndexes(typeof(T));
        }

        public IStore RegisterIndexes(Type type)
        {
            var index = Activator.CreateInstance(type) as IIndexProvider;
            if (index != null)
            {
                Indexes.Add(index);
            }

            return this;
        }

        public IStore RegisterIndexes(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                RegisterIndexes(type);
            }

            return this;
        }

        public IStore RegisterIndexes(Assembly assembly)
        {
            var exportedTypes = assembly.GetExportedTypes();
            var indexes = exportedTypes.Where(x => typeof(IIndexProvider).IsAssignableFrom(x));
            return RegisterIndexes(indexes);
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

            return Descriptors.GetOrAdd(target, key =>
            {
                var contextType = typeof(DescribeContext<>).MakeGenericType(target);
                var context = Activator.CreateInstance(contextType) as IDescriptor;

                foreach (var provider in Indexes)
                {
                    if (provider.ForType().IsAssignableFrom(target))
                    {
                        provider.Describe(context);
                    }
                }

                return context.Describe(new[] { target }).ToList();
            });
        }

        public int GetNextId()
        {
            return (int)IdGenerator.GetNextId();
        }
    }
}
