using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace YesSql.Provider.SqlServer
{
    public static class SqlServerDbProviderOptionsExtensions
    {
        public static IConfiguration UseSqlServer(
            this IConfiguration configuration,
            string connectionString,
            string schema = null) => UseSqlServer(configuration, connectionString, IsolationLevel.ReadUncommitted, schema);

        public static IConfiguration UseSqlServer(
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

            configuration.SqlDialect = new SqlServerDialect();
            configuration.Schema = schema;
            configuration.CommandInterpreter = new SqlServerCommandInterpreter(configuration);
            configuration.ConnectionFactory = new DbConnectionFactory<SqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
