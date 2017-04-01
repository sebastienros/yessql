using Npgsql;
using System;
using System.Data;
using YesSql.Core.Services;
using YesSql.Storage.Cache;
using YesSql.Storage.Sql;

namespace YesSql.Provider.PostgreSql
{
    public static class PostgreSqlDbProviderOptionsExtensions
    {
        public static void UsePostgreSql(
            this Configuration configuration,
            string connectionString,
            bool cached = false)
        {
            UsePostgreSql(configuration, connectionString, IsolationLevel.ReadUncommitted, cached);
        }

        public static void UsePostgreSql(
            this Configuration configuration,
            string connectionString,
            IsolationLevel isolationLevel)
        {
            UsePostgreSql(configuration, connectionString, IsolationLevel.ReadUncommitted, cached: false);
        }

        public static void UsePostgreSql(
            this Configuration configuration,
            string connectionString,
            IsolationLevel isolationLevel,
            bool cached)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            configuration.ConnectionFactory = new DbConnectionFactory<NpgsqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            if (cached)
            {
                configuration.DocumentStorageFactory =
                    new CacheDocumentStorageFactory(new SqlDocumentStorageFactory());
            }
            else
            {
                configuration.DocumentStorageFactory =
                    new CacheDocumentStorageFactory(new SqlDocumentStorageFactory());
            }
        }
    }
}
