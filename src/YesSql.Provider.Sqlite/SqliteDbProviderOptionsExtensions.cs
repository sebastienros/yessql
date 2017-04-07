using Microsoft.Data.Sqlite;
using System;
using System.Data;
using YesSql.Core.Services;
using YesSql.Storage.Cache;
using YesSql.Storage.Sql;

namespace YesSql.Provider.Sqlite
{
    public static class SqliteDbProviderOptionsExtensions
    {
        public static void UseSqLite(
            this Configuration configuration,
            string connectionString,
            bool cached = false)
        {
            UseSqLite(configuration, connectionString, IsolationLevel.Serializable, cached);
        }

        public static void UseSqLite(
            this Configuration configuration,
            string connectionString,
            IsolationLevel isolationLevel)
        {
            UseSqLite(configuration, connectionString, IsolationLevel.Serializable, cached: false);
        }

        public static void UseSqLite(
            this Configuration configuration,
            string connectionString,
            IsolationLevel isolationLevel,
            bool cached = false)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            configuration.ConnectionFactory = new DbConnectionFactory<SqliteConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            if (cached)
            {
                configuration.DocumentStorageFactory =
                    new CacheDocumentStorageFactory(new SqlDocumentStorageFactory());
            }
            else
            {
                configuration.DocumentStorageFactory = new SqlDocumentStorageFactory();
            }
        }
    }
}
