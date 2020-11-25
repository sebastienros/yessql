using System;
using System.Data;
using MySqlConnector;

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

            configuration.SqlDialect = new MySqlDialect();
            configuration.CommandInterpreter = new MySqlCommandInterpreter(configuration.SqlDialect);
            configuration.ConnectionFactory = new DbConnectionFactory<MySqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
