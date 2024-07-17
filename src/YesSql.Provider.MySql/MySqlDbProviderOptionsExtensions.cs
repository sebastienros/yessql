using MySqlConnector;
using System;
using System.Data;

namespace YesSql.Provider.MySql
{
    public static class MySqlDbProviderOptionsExtensions
    {
        public static IConfiguration UseMySql(
            this IConfiguration configuration,
            string connectionString,
            string schema = null) => UseMySql(configuration, connectionString, IsolationLevel.ReadUncommitted, schema);

        public static IConfiguration UseMySql(
            this IConfiguration configuration,
            string connectionString,
            IsolationLevel isolationLevel,
            string schema = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            configuration.SqlDialect = new MySqlDialect();
            configuration.Schema = schema;
            configuration.CommandInterpreter = new MySqlCommandInterpreter(configuration);
            configuration.ConnectionFactory = new DbConnectionFactory<MySqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
