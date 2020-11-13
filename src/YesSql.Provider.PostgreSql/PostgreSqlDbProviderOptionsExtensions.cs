using Npgsql;
using System;
using System.Data;

namespace YesSql.Provider.PostgreSql
{
    public static class PostgreSqlDbProviderOptionsExtensions
    {
        public static IConfiguration UsePostgreSql(
            this IConfiguration configuration,
            string connectionString)
        {
            return UsePostgreSql(configuration, connectionString, IsolationLevel.ReadUncommitted);
        }

        public static IConfiguration UsePostgreSql(
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

            configuration.SqlDialect = new PostgreSqlDialect();
            configuration.CommandInterpreter = new PostgreSqlCommandInterpreter(configuration.SqlDialect);
            configuration.ConnectionFactory = new DbConnectionFactory<NpgsqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
