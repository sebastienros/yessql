using System;
using System.Collections.Generic;
using System.Reflection;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Automapping;
using NHibernate.Cfg;
using YesSql.Core.Data;
using YesSql.Core.Indexes;
using YesSql.Core.Serialization;
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
        /// Creates an NHibernate configuration using custom mappings
        /// </summary>
        Configuration CreateConfiguration(Func<IPersistenceConfigurer> config, Func<AutoPersistenceModel, AutoPersistenceModel> mapping);

        /// <summary>
        /// Creates a named NHibernate configuration using automatic mappings
        /// </summary>
        Configuration CreateConfiguration(string name, Func<IPersistenceConfigurer> config);

        /// <summary>
        /// Creates a named NHibernate configuration using custom mappings
        /// </summary>
        Configuration CreateConfiguration(string name, Func<IPersistenceConfigurer> config, Func<AutoPersistenceModel, AutoPersistenceModel> mapping);

        /// <summary>
        /// Configures the store using default settings
        /// </summary>
        IStore Configure(IPersistenceConfigurer config);

        /// <summary>
        /// Configures the store
        /// </summary>
        IStore Configure(IPersistenceConfigurer config, Action<Configuration> init);

        /// Configures the store
        IStore Configure(Action<IStore> cfg);

        /// Configures the store
        IStore Configure(Configuration cfg);

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

        /// <summary>
        /// Registers an <see cref="IDocumentSerializerFactory"/> implementation providing
        /// <see cref="IDocumentSerializer"/>
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of the factory.</typeparam>
        IStore RegisterSerializer<T>() where T : IDocumentSerializerFactory;
        IStore RegisterSerializer(Type type);

        IDocumentSerializer GetDocumentSerializer();

        IIdAccessor GetIdAccessor(Type tContainer, string name);

    }
}
