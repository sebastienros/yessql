using MySql.Data.MySqlClient;
using System;
using System.Data;
using YesSql.Core.Services;
using YesSql.Storage.Sql;

namespace YesSql.Provider.MySql
{
    public static class MySqlDbProviderOptionsExtensions
    {
        public static void UseMySql(
            this Configuration configuration,
            string connectionString)
        {
            UseMySql(configuration, connectionString, IsolationLevel.ReadUncommitted);
        }

        public static void UseMySql(
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
                ConnectionFactory = new DbConnectionFactory<MySqlConnection>(connectionString, true),
                DocumentStorageFactory = new SqlDocumentStorageFactory(),
                IsolationLevel = isolationLevel
            };
        }
    }
}
