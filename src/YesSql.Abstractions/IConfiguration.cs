using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace YesSql
{
    public interface IConfiguration
    {
        IIdentifierFactory IdentifierFactory { get; set; }
        IsolationLevel IsolationLevel { get; set; }
        IConnectionFactory ConnectionFactory { get; set; }
        IContentSerializer ContentSerializer { get; set; }
        IIdGenerator IdGenerator { get; set; }
        ILogger Logger { get; set; }
        string TablePrefix { get; set; }
        int SessionPoolSize { get; set; }
        bool QueryGatingEnabled { get; set; }
        HashSet<Type> ConcurrentTypes { get; }
    }

    public static class ConfigurationExtensions
    {
        public static IConfiguration SetIdentifierFactory(this IConfiguration configuration, IIdentifierFactory identifierFactory)
        {
            configuration.IdentifierFactory = identifierFactory;
            return configuration;
        }

        public static IConfiguration SetIsolationLevel(this IConfiguration configuration, IsolationLevel isolationLevel)
        {
            configuration.IsolationLevel = isolationLevel;
            return configuration;
        }

        public static IConfiguration SetConnectionFactory(this IConfiguration configuration, IConnectionFactory connectionFactory)
        {
            configuration.ConnectionFactory = connectionFactory;
            return configuration;
        }

        public static IConfiguration SetTablePrefix(this IConfiguration configuration, string tablePrefix)
        {
            configuration.TablePrefix = tablePrefix;
            return configuration;
        }

        public static IConfiguration SetContentSerializer(this IConfiguration configuration, IContentSerializer contentSerializer)
        {
            configuration.ContentSerializer = contentSerializer;
            return configuration;
        }

        public static IConfiguration SetSessionPoolSize(this IConfiguration configuration, int size)
        {
            configuration.SessionPoolSize = size;
            return configuration;
        }

        public static IConfiguration DisableQueryGating(this IConfiguration configuration)
        {
            configuration.QueryGatingEnabled = false;
            return configuration;
        }

        public static IConfiguration UseLogger(this IConfiguration configuration, ILogger logger)
        {
            configuration.Logger = logger;
            return configuration;
        }

        public static IConfiguration CheckConcurrentUpdates(this IConfiguration configuration, Type type)
        {
            configuration.ConcurrentTypes.Add(type);
            return configuration;
        }

        public static IConfiguration CheckConcurrentUpdates<T>(this IConfiguration configuration)
        {
            return CheckConcurrentUpdates(configuration, typeof(T));
        }
    }

    public class DbConnectionFactory<TDbConnection> : IConnectionFactory
        where TDbConnection : DbConnection, new()
    {
        private readonly string _connectionString;

        public Type DbConnectionType => typeof(TDbConnection);

        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbConnection CreateConnection()
        {
            var connection = new TDbConnection
            {
                ConnectionString = _connectionString
            };

            return connection;
        }
    }
}
