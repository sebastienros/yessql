using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Core.Data;
using System.Reflection;
using YesSql.Core.Sql;

namespace YesSql.Core.Services
{
    public class Store : IStore
    {
        protected readonly IList<IIndexProvider> Indexes;
        internal readonly Configuration _configuration;

        internal readonly ConcurrentDictionary<Type, Func<Index, object>> GroupMethods =
            new ConcurrentDictionary<Type, Func<Index, object>>();

        internal readonly ConcurrentDictionary<Type, IEnumerable<IndexDescriptor>> Descriptors =
            new ConcurrentDictionary<Type, IEnumerable<IndexDescriptor>>();

        internal readonly ConcurrentDictionary<Type, IIdAccessor> _idAccessors =
            new ConcurrentDictionary<Type, IIdAccessor>();


        public Store(Action<Configuration> cfg)
        {
            _configuration = new Configuration();
            Indexes = new List<IIndexProvider>();
            cfg(_configuration);

            ValidateConfiguration();

            var connection = _configuration.ConnectionFactory.CreateConnection();
            connection.Open();
            try
            {
                using (var transaction = connection.BeginTransaction(_configuration.IsolationLevel))
                {
                    var schemaBuilder = new SchemaBuilder(connection);

                    // Document
                    // This table should be part of the default migration code, and 
                    // its version also created in the migration table

                    schemaBuilder
                        .CreateTable("Document", table => table
                            .Column<int>("Id", column => column.Identity().NotNull())
                            .Column<string>("Type", column => column.NotNull())
                        )
                        .AlterTable("Document", table => table
                            .CreateIndex("IX_Type", "Type")
                        );

                    foreach (var migration in _configuration.Migrations)
                    {
                        if (migration != null)
                        {
                            migration(schemaBuilder);
                        }
                    }

                    transaction.Commit();
                }
            }
            finally
            {
                if (_configuration.ConnectionFactory.Disposable)
                {
                    connection.Dispose();
                }
            }
        }

        private void ValidateConfiguration()
        {
            if (_configuration.ConnectionFactory == null)
            {
                throw new ApplicationException("The connection factory should be initialized during configuration.");
            }

            if (_configuration.DocumentStorageFactory == null)
            {
                throw new ApplicationException("The document storage factory should be initialized during configuration.");
            }
        }

        public ISession CreateSession(bool trackChanges = true)
        {
            var storage = _configuration.DocumentStorageFactory.CreateDocumentStorage();
            return new Session(storage, trackChanges, this);
        }

        public void Dispose()
        {
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

        public IIdAccessor GetIdAccessor(Type tContainer, string name)
        {
            return _idAccessors.GetOrAdd(tContainer, type => _configuration.IdentifierFactory.CreateAccessor(tContainer, name));
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
    }
}
