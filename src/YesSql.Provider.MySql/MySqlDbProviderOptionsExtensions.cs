using MySql.Data.MySqlClient;
using System;
using System.Data;
using YesSql.Core.Services;
using YesSql.Storage.Cache;
using YesSql.Storage.Sql;

namespace YesSql.Provider.MySql
{
    public static class MySqlDbProviderOptionsExtensions
    {
        public static void UseMySql(
            this Configuration configuration,
            string connectionString,
            bool cached = false)
        {
            UseMySql(configuration, connectionString, IsolationLevel.ReadUncommitted, cached);
        }

        public static void UseMySql(
            this Configuration configuration,
            string connectionString,
            IsolationLevel isolationLevel)
        {
            UseMySql(configuration, connectionString, isolationLevel, cached: false);
        }

        public static void UseMySql(
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

            configuration.ConnectionFactory = new DbConnectionFactory<MySqlConnection>(connectionString);
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
