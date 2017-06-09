using Npgsql;
using System;
using System.Data;

namespace YesSql.Provider.PostgreSql
{
    public static class PostgreSqlDbProviderOptionsExtensions
    {
        public static readonly PostgreSqlDialect DefaulPostgreSqlDialect = new PostgreSqlDialect();

        public static IConfiguration RegisterPostgreSql(this IConfiguration configuration)
        {
            return RegisterPostgreSql(configuration, DefaulPostgreSqlDialect);
        }

        public static IConfiguration RegisterPostgreSql(this IConfiguration configuration, PostgreSqlDialect postgreSqlDialect)
        {
            SqlDialectFactory.SqlDialects["npgsqlconnection"] = postgreSqlDialect;
            CommandInterpreterFactory.CommandInterpreters["npgsqlconnection"] = d => new PostgreSqlCommandInterpreter(d);

            return configuration;
        }

        public static IConfiguration UsePostgreSql(
            this IConfiguration configuration,
            string connectionString)
        {
            return UsePostgreSql(configuration, connectionString, IsolationLevel.ReadUncommitted);
        }

        public static IConfiguration UsePostgreSql(
            this IConfiguration configuration,
            string connectionString,
            PostgreSqlDialect postgreSqlDialect)
        {
            return UsePostgreSql(configuration, connectionString, postgreSqlDialect, IsolationLevel.ReadUncommitted);
        }

        public static IConfiguration UsePostgreSql(
            this IConfiguration configuration,
            string connectionString,
            IsolationLevel isolationLevel)
        {
            return UsePostgreSql(configuration, connectionString, DefaulPostgreSqlDialect, isolationLevel);
        }

        public static IConfiguration UsePostgreSql(
            this IConfiguration configuration,
            string connectionString,
            PostgreSqlDialect postgreSqlDialect,
            IsolationLevel isolationLevel)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            if (postgreSqlDialect == null)
            {
                throw new ArgumentNullException(nameof(postgreSqlDialect));
            }

            RegisterPostgreSql(configuration, postgreSqlDialect);
            configuration.ConnectionFactory = new DbConnectionFactory<NpgsqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
