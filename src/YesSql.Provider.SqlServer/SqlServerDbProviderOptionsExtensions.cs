using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace YesSql.Provider.SqlServer
{
    public static class SqlServerDbProviderOptionsExtensions
    {
        public static IConfiguration UseSqlServer(
            this IConfiguration configuration,
            string connectionString)
        {
            return UseSqlServer(configuration, connectionString, IsolationLevel.ReadUncommitted);
        }

        public static IConfiguration UseSqlServer2008(
            this IConfiguration configuration,
            string connectionString)
        {
            return UseSqlServer2008(configuration, connectionString, IsolationLevel.ReadUncommitted);
        }
        public static IConfiguration UseSqlServer(
            this IConfiguration configuration,
            string connectionString,
            IsolationLevel isolationLevel)
        {
            return UseSqlServer(configuration, connectionString, isolationLevel, new SqlServerDialect());
        }
        public static IConfiguration UseSqlServer2008(
            this IConfiguration configuration,
            string connectionString,
            IsolationLevel isolationLevel)
        {
            return UseSqlServer(configuration, connectionString, isolationLevel, new SqlServer2008Dialect());
        }
        private static IConfiguration UseSqlServer(
        this IConfiguration configuration,
        string connectionString,
        IsolationLevel isolationLevel,
        ISqlDialect sqlDialect)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            configuration.SqlDialect = sqlDialect;
            configuration.CommandInterpreter = new SqlServerCommandInterpreter(configuration.SqlDialect);
            configuration.ConnectionFactory = new DbConnectionFactory<SqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
