using System;
using System.Data;
using System.Data.SqlClient;
using YesSql.Core.Provider;
using YesSql.Core.Services;
using YesSql.Storage.Sql;

namespace YesSql.Provider.SqlServer
{
    public static class SqlServerDbProviderOptionsExtensions
    {
        public static void UseSqlServer(
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
                ConnectionFactory = new DbConnectionFactory<SqlConnection>(connectionString, true),
                DocumentStorageFactory = new SqlDocumentStorageFactory(),
                IsolationLevel = IsolationLevel.ReadUncommitted
            };

            options.ProviderName = "SQL Server";
            options.Configuration = configuration;
        }
    }
}
