using MySql.Data.MySqlClient;
using System;
using System.Data;
using YesSql.Core.Provider;
using YesSql.Core.Services;
using YesSql.Storage.Sql;

namespace YesSql.Provider.MySql
{
    public static class MySqlDbProviderOptionsExtensions
    {
        public static void UseMySql(
            this IDbProviderOptions options,
            string connectionString)
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
                ConnectionFactory = new DbConnectionFactory<MySqlConnection>(connectionString, true),
                DocumentStorageFactory = new SqlDocumentStorageFactory(),
                IsolationLevel = IsolationLevel.ReadUncommitted
            };

            options.ProviderName = "MySQL";
            options.Configuration = configuration;
        }
    }
}
