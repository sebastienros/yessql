using System;
using System.Data;
using System.Data.SqlClient;
using YesSql.Core.Services;
using YesSql.Storage.Sql;

namespace YesSql.Provider.SqlServer
{
    public static class SqlServerDbProviderOptionsExtensions
    {
        public static void UseSqlServer(
            this Configuration configuration,
            string connectionString)
        {
            UseSqlServer(configuration, connectionString, IsolationLevel.ReadUncommitted);
        }

        public static void UseSqlServer(
            this Configuration configuration,
            string connectionString,
            IsolationLevel isolationLevel)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<SqlConnection>(connectionString),
                DocumentStorageFactory = new SqlDocumentStorageFactory(),
                IsolationLevel = isolationLevel
            };
        }
    }
}
