using MySql.Data.MySqlClient;
using System;
using System.Data;
using YesSql.Services;
using YesSql.Storage.Sql;

namespace YesSql.Provider.MySql
{
    public static class MySqlDbProviderOptionsExtensions
    {
        public static IConfiguration UseMySql(
            this IConfiguration configuration,
            string connectionString)
        {
            return UseMySql(configuration, connectionString, IsolationLevel.ReadUncommitted);
        }

        public static IConfiguration UseMySql(
            this IConfiguration configuration,
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

            configuration.ConnectionFactory = new DbConnectionFactory<MySqlConnection>(connectionString);
            configuration.DocumentStorageFactory = new SqlDocumentStorageFactory();
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
