using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using YesSql.Core.Data.Mappings;
using YesSql.Core.Indexes;
using YesSql.Core.Services;
using YesSql.Core.Sharding;
using ISession = YesSql.Core.Services.ISession;

namespace YesSql.Core.Data
{
    public class Store : IStore
    {
        private const string DefaultConfigurationName = "Default";

        private readonly object _synLock = new object();
        private Action<Store> _sessionFactoryInitializer;
        private readonly Dictionary<string, Configuration> _configurations;
        private Dictionary<string, ISessionFactory> _sessionFactories;
        internal readonly IList<IIndexProvider> Indexes;
        private IShardStrategyFactory _shardStrategyFactory;

        internal readonly ConcurrentDictionary<Type, Func<IIndex, object>> GroupMethods =
            new ConcurrentDictionary<Type, Func<IIndex, object>>();

        internal readonly ConcurrentDictionary<Type, IEnumerable<IndexDescriptor>> Descriptors =
            new ConcurrentDictionary<Type, IEnumerable<IndexDescriptor>>();

        public Store()
        {
            Indexes = new List<IIndexProvider>();
            _configurations = new Dictionary<string, Configuration>();
        }

        public Configuration CreateConfiguration(Func<IPersistenceConfigurer> config)
        {
            return CreateConfiguration(DefaultConfigurationName, config);
        }

        public Configuration CreateConfiguration(string name, Func<IPersistenceConfigurer> config)
        {
            var typeSource = new IndexTypeSource(Indexes);

            // finally we add customization through IAutoMappingAlterations
            var model = AutoMap
                // add all indexes to auto-mapping
                .Source(typeSource)
                // add Document to auto-mapping
                .AddTypeSource(new DocumentTypeSource())
                .Alterations(
                    alt => alt
                        // map fake relationships from document to their indexes
                        .Add(new DocumentAlteration(typeSource))
                        // map relationships from any indexes to their documents
                        .Add(new IndexAlteration(typeSource))
                );

            return _configurations[name] = Fluently.Configure()
                .Mappings(m =>m.AutoMappings.Add(model)
                    // .ExportTo(@"c:\temp\") // export the mappings
                )
                .Database(config)
                .BuildConfiguration()
                ;
        }

        public IStore SetShardingStrategy(IShardStrategyFactory shardStrategyFactory)
        {
            _shardStrategyFactory = shardStrategyFactory;
            return this;
        }

        public IStore Configure(IPersistenceConfigurer config)
        {
            return Configure(store =>
            {
                CreateConfiguration(() => config);
                foreach(var configuration  in _configurations.Values)
                {
                    new SchemaUpdate(configuration).Execute(false, true);
                }
                
            });
        }

        public IStore Configure(Action<IStore> cfg)
        {
            _sessionFactoryInitializer = cfg;
            return this;
        }

        public ISession CreateSession()
        {
            if (_sessionFactories == null)
            {
                _sessionFactories = new Dictionary<string, ISessionFactory>();

                // locking to prevent several threads from initializing the same store
                // as a Store instance should be static
                lock (_synLock)
                {
                    _sessionFactoryInitializer(this);
                    foreach (string configurationName in _configurations.Keys)
                    {
                        _sessionFactories.Add(configurationName, _configurations[configurationName].BuildSessionFactory());
                    }
                }
            }

            if (_sessionFactories == null || !_sessionFactories.Any())
            {
                // removes compilation warning about accessing a null property
                throw new ApplicationException("The session factory should have been initialized during configuration.");
            }

            var sessions = _sessionFactories.ToDictionary(s => s.Key, s => (ISession) new Session(s.Value.OpenSession(), this));

            // if multiple sessions factories are available, return a sharding session
            if(sessions.Count > 1 )
            {
                return new ShardingSession(sessions, _shardStrategyFactory.Create(_sessionFactories.Keys), this);
            }

            // otherwise, return unique session
            return sessions.Values.Single();
        }

        public void Dispose()
        {
            foreach (var sessionFactory in _sessionFactories.Values)
            {
                sessionFactory.Dispose();
            }
        }

        public IStore RegisterIndexes<T>() where T : IIndexProvider
        {
            return RegisterIndexes(typeof (T));
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

        public IStore RegisterIndexes(System.Reflection.Assembly assembly)
        {
            var exportedTypes = assembly.GetExportedTypes();
            var indexes = exportedTypes.Where(x => typeof (IIndexProvider).IsAssignableFrom(x));
            return RegisterIndexes(indexes);
        }
    }
}
