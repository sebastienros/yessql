using Microsoft.Data.Sqlite;
using System;
using System.Data;
using YesSql.Core.Services;
using YesSql.Storage.Sql;

namespace YesSql.Provider.Sqlite
{
    public static class SqliteDbProviderOptionsExtensions
    {
        public static void UseSqLite(
            this Configuration configuration,
            string connectionString)
        {
            UseSqLite(configuration, connectionString, IsolationLevel.Serializable);
        }

        public static void UseSqLite(
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
                ConnectionFactory = new DbConnectionFactory<SqliteConnection>(connectionString, true),
                DocumentStorageFactory = new SqlDocumentStorageFactory(),
                IsolationLevel = isolationLevel
            };
        }
    }
}
