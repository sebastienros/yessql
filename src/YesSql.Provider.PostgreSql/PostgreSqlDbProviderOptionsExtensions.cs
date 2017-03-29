using Npgsql;
using System;
using System.Data;
using YesSql.Core.Provider;
using YesSql.Core.Services;
using YesSql.Storage.Sql;

namespace YesSql.Provider.PostgreSql
{
    public static class PostgreSqlDbProviderOptionsExtensions
    {
        public static void UsePostgreSql(
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
                ConnectionFactory = new DbConnectionFactory<NpgsqlConnection>(connectionString, true),
                DocumentStorageFactory = new SqlDocumentStorageFactory(),
                IsolationLevel = IsolationLevel.ReadUncommitted
            };

            options.ProviderName = "PostgreSQL";
            options.Configuration = configuration;
        }
    }
}
