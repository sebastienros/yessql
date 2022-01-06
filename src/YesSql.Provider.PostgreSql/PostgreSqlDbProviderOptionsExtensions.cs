using Npgsql;
using System;
using System.Data;

namespace YesSql.Provider.PostgreSql
{
    public static class PostgreSqlDbProviderOptionsExtensions
    {
        public static IConfiguration UsePostgreSql(
            this IConfiguration configuration,
            string connectionString,
            string schema = null)
        {
            return UsePostgreSql(configuration, connectionString, IsolationLevel.ReadUncommitted, schema);
        }

        public static IConfiguration UsePostgreSql(
            this IConfiguration configuration,
            string connectionString,
            IsolationLevel isolationLevel,
            string schema = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            configuration.SqlDialect = new PostgreSqlDialect(schema);
            configuration.CommandInterpreter = new PostgreSqlCommandInterpreter(configuration.SqlDialect);
            configuration.ConnectionFactory = new DbConnectionFactory<NpgsqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
