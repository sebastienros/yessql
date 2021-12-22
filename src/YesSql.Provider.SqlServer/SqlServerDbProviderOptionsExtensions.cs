using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace YesSql.Provider.SqlServer
{
    public static class SqlServerDbProviderOptionsExtensions
    {
        private const string DefaultSchema = "dbo";

        public static IConfiguration UseSqlServer(
            this IConfiguration configuration,
            string connectionString,
            string schema = DefaultSchema)
        {
            return UseSqlServer(configuration, connectionString, IsolationLevel.ReadUncommitted, schema);
        }

        public static IConfiguration UseSqlServer(
            this IConfiguration configuration,
            string connectionString,
            IsolationLevel isolationLevel,
            string schema = DefaultSchema)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            configuration.SqlDialect = new SqlServerDialect(schema);
            configuration.CommandInterpreter = new SqlServerCommandInterpreter(configuration.SqlDialect);
            configuration.ConnectionFactory = new DbConnectionFactory<SqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
