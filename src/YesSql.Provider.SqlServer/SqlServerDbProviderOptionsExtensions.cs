using System;
using System.Data;
using System.Data.SqlClient;

namespace YesSql.Provider.SqlServer
{
    public static class SqlServerDbProviderOptionsExtensions
    {
        public static readonly SqlServerDialect DefaulSqlServerDialect = new SqlServerDialect();

        public static IConfiguration RegisterSqlServer(this IConfiguration configuration)
        {
            return RegisterSqlServer(configuration, DefaulSqlServerDialect);
        }

        public static IConfiguration RegisterSqlServer(this IConfiguration configuration, SqlServerDialect sqlServerDialect)
        {
            SqlDialectFactory.SqlDialects["sqlconnection"] = sqlServerDialect;
            CommandInterpreterFactory.CommandInterpreters["sqlconnection"] = d => new SqlServerCommandInterpreter(d);

            return configuration;
        }

        public static IConfiguration UseSqlServer(
            this IConfiguration configuration,
            string connectionString)
        {
            return UseSqlServer(configuration, connectionString, DefaulSqlServerDialect);
        }

        public static IConfiguration UseSqlServer(
            this IConfiguration configuration,
            string connectionString,
            SqlServerDialect sqlServerDialect)
        {
            return UseSqlServer(configuration, connectionString, sqlServerDialect, IsolationLevel.ReadUncommitted);
        }

        public static IConfiguration UseSqlServer(
            this IConfiguration configuration,
            string connectionString,
            IsolationLevel isolationLevel)
        {
            return UseSqlServer(configuration, connectionString, DefaulSqlServerDialect, isolationLevel);
        }

        public static IConfiguration UseSqlServer(
            this IConfiguration configuration,
            string connectionString,
            SqlServerDialect sqlServerDialect,
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

            if (sqlServerDialect == null)
            {
                throw new ArgumentNullException(nameof(sqlServerDialect));
            }

            RegisterSqlServer(configuration, sqlServerDialect);
            configuration.ConnectionFactory = new DbConnectionFactory<SqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
