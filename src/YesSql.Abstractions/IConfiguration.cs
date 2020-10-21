using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace YesSql
{
    /// <summary>
    /// Represents the state of YesSql configuration.
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// Gets or sets the <see cref="IAccessorFactory" /> instance for identifiers.
        /// </summary>
        IAccessorFactory IdentifierAccessorFactory { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IAccessorFactory" /> instance for versions.
        /// </summary>
        IAccessorFactory VersionAccessorFactory { get; set; }

        /// <summary>
        /// Gets or sets the isolation level of transactions.
        /// </summary>
        IsolationLevel IsolationLevel { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IConnectionFactory" />
        /// </summary>
        IConnectionFactory ConnectionFactory { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IContentSerializer" /> instance used to serialize documents.
        /// </summary>
        IContentSerializer ContentSerializer { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IIdGenerator" /> instance used to generate unique identifiers.
        /// </summary>
        IIdGenerator IdGenerator { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ILogger" /> instance.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the table prefix.
        /// </summary>
        string TablePrefix { get; set; }

        /// <summary>
        /// Gets or sets the session pool size.
        /// </summary>
        int SessionPoolSize { get; set; }

        /// <summary>
        /// Gets or sets the command page size.
        /// </summary>
        int CommandsPageSize { get; set; }

        /// <summary>
        /// Gets or sets whether the query gating feature is enabled.
        /// </summary>
        bool QueryGatingEnabled { get; set; }

        /// <summary>
        /// Gets the collection of types that must be checked for concurrency.
        /// </summary>
        HashSet<Type> ConcurrentTypes { get; }

        /// <summary>
        /// Gets or sets the <see cref="ITableNameConvention" /> instance.
        /// </summary>
        ITableNameConvention TableNameConvention { get; set; }
    }

    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Sets the <see cref="IAccessorFactory" /> instances used to access the identifier of an object.
        /// </summary>
        public static IConfiguration SetIdentifierAccessorFactory(this IConfiguration configuration, IAccessorFactory identifierAccessorFactory)
        {
            configuration.IdentifierAccessorFactory = identifierAccessorFactory;
            return configuration;
        }

        /// <summary>
        /// Sets the <see cref="IAccessorFactory" /> instances used to access the version of an object.
        /// </summary>
        public static IConfiguration SetVersionAccessorFactory(this IConfiguration configuration, IAccessorFactory versionAccessorFactory)
        {
            configuration.VersionAccessorFactory = versionAccessorFactory;
            return configuration;
        }

        /// <summary>
        /// Sets the isolation level of transactions.
        /// </summary>
        public static IConfiguration SetIsolationLevel(this IConfiguration configuration, IsolationLevel isolationLevel)
        {
            configuration.IsolationLevel = isolationLevel;
            return configuration;
        }

        /// <summary>
        /// Sets the <see cref="IConnectionFactory" /> instance.
        /// </summary>
        public static IConfiguration SetConnectionFactory(this IConfiguration configuration, IConnectionFactory connectionFactory)
        {
            configuration.ConnectionFactory = connectionFactory;
            return configuration;
        }

        /// <summary>
        /// Sets the table prefix.
        /// </summary>
        public static IConfiguration SetTablePrefix(this IConfiguration configuration, string tablePrefix)
        {
            configuration.TablePrefix = tablePrefix;
            return configuration;
        }

        /// <summary>
        /// Sets the <see cref="IContentSerializer" /> instance.
        /// </summary>
        public static IConfiguration SetContentSerializer(this IConfiguration configuration, IContentSerializer contentSerializer)
        {
            configuration.ContentSerializer = contentSerializer;
            return configuration;
        }

        /// <summary>
        /// Sets the session pool size.
        /// </summary>
        public static IConfiguration SetSessionPoolSize(this IConfiguration configuration, int size)
        {
            configuration.SessionPoolSize = size;
            return configuration;
        }

        /// <summary>
        /// Sets the commands pages size.
        /// </summary>
        public static IConfiguration SetCommandsPageSize(this IConfiguration configuration, int size)
        {
            configuration.CommandsPageSize = size;
            return configuration;
        }

        /// <summary>
        /// Disables query gating.
        /// </summary>
        public static IConfiguration DisableQueryGating(this IConfiguration configuration)
        {
            configuration.QueryGatingEnabled = false;
            return configuration;
        }

        /// <summary>
        /// Sets the <see cref="ILogger" /> instance.
        /// </summary>
        public static IConfiguration UseLogger(this IConfiguration configuration, ILogger logger)
        {
            configuration.Logger = logger;
            return configuration;
        }

        /// <summary>
        /// Configures a type to be checked for optimistic concurrency.
        /// </summary>
        public static IConfiguration CheckConcurrentUpdates(this IConfiguration configuration, Type type)
        {
            configuration.ConcurrentTypes.Add(type);
            return configuration;
        }

        /// <summary>
        /// Configures a type to be checked for optimistic concurrency.
        /// </summary>
        public static IConfiguration CheckConcurrentUpdates<T>(this IConfiguration configuration)
        {
            return CheckConcurrentUpdates(configuration, typeof(T));
        }

        /// <summary>
        /// Sets the <see cref="ITableNameConvention" /> instance.
        /// </summary>
        public static IConfiguration SetTableNameConvention(this IConfiguration configuration, ITableNameConvention convention)
        {
            configuration.TableNameConvention = convention;
            return configuration;
        }
    }
}
