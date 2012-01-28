using System;
using System.Collections.Generic;
using System.Reflection;
using FluentNHibernate.Cfg.Db;
using NHibernate.Cfg;
using YesSql.Core.Indexes;
using YesSql.Core.Sharding;

namespace YesSql.Core.Services
{
    public interface IStore : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="ISession"/> to communicate with the <see cref="IStore"/>
        /// </summary>
        ISession CreateSession();

        /// <summary>
        /// Creates an NHibernate configuration using automatic mappings
        /// </summary>
        Configuration CreateConfiguration(Func<IPersistenceConfigurer> config);

        /// <summary>
        /// Creates a named NHibernate configuration using automatic mappings
        /// </summary>
        Configuration CreateConfiguration(string name, Func<IPersistenceConfigurer> config);

        /// <summary>
        /// Configures the store using default settings
        /// </summary>
        IStore Configure(IPersistenceConfigurer config);

        /// Configures the store
        IStore Configure(Action<IStore> cfg);

        /// <summary>
        /// Defines the <see cref="IShardStrategyFactory"/> to use
        /// </summary>
        IStore SetShardingStrategy(IShardStrategyFactory shardStrategyFactory);

        /// <summary>
        /// Registers an index using an <see cref="IIndexProvider"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        IStore RegisterIndexes<T>() where T : IIndexProvider;
        IStore RegisterIndexes(Type type);
        IStore RegisterIndexes(IEnumerable<Type> types);
        IStore RegisterIndexes(Assembly assembly);
    }
}
