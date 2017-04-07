using System;
using System.Data;
using System.Data.SqlClient;
using YesSql.Core.Services;
using YesSql.Storage.Cache;
using YesSql.Storage.Sql;

namespace YesSql.Provider.SqlServer
{
    public static class SqlServerDbProviderOptionsExtensions
    {
        public static void UseSqlServer(
            this Configuration configuration,
            string connectionString,
            bool cached = false)
        {
            UseSqlServer(configuration, connectionString, IsolationLevel.ReadUncommitted, cached);
        }

        public static void UseSqlServer(
            this Configuration configuration,
            string connectionString,
            IsolationLevel isolationLevel)
        {
            UseSqlServer(configuration, connectionString, IsolationLevel.ReadUncommitted, cached: false);
        }

        public static void UseSqlServer(
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

            configuration.ConnectionFactory = new DbConnectionFactory<SqlConnection>(connectionString);
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
