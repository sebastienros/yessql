using Microsoft.Data.Sqlite;
using System;
using System.Data;
using YesSql.Core.Provider;
using YesSql.Core.Services;
using YesSql.Storage.Sql;

namespace YesSql.Provider.Sqlite
{
    public static class SqliteDbProviderOptionsExtensions
    {
        public static void UseSqLite(
            this IDbProviderOptions options,
            string connectionString)
        {
            UseSqLite(options, connectionString, IsolationLevel.Serializable);
        }

        public static void UseSqLite(
            this IDbProviderOptions options,
            string connectionString,
            IsolationLevel isolationLevel)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            var configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<SqliteConnection>(connectionString, true),
                DocumentStorageFactory = new SqlDocumentStorageFactory(),
                IsolationLevel = isolationLevel
            };

            options.ProviderName = "SQLite";
            options.Configuration = configuration;
        }
    }
}
