using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace YesSql.Provider.SqlServer
{
    /// <summary>
    /// Provides extension methods on <see cref="IConfiguration"/> to configure YesSql to use the SQL Server provider.
    /// </summary>
    public static class SqlServerDbProviderOptionsExtensions
    {
        /// <summary>
        /// Configures YesSql to use the SQL Server document database provider.
        /// </summary>
        /// <param name="configuration">The configuration to register the provider on.</param>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        /// <param name="schema">The optional database schema to use.</param>
        /// <returns>The <paramref name="configuration"/> instance to allow chaining.</returns>
        public static IConfiguration UseSqlServer(
            this IConfiguration configuration,
            string connectionString,
            string schema = null)
        {
            return UseSqlServer(configuration, connectionString, IsolationLevel.ReadUncommitted, schema);
        }

        /// <summary>
        /// Configures YesSql to use the SQL Server document database provider.
        /// </summary>
        /// <param name="configuration">The configuration to register the provider on.</param>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        /// <param name="isolationLevel">The isolation level used for transactions.</param>
        /// <param name="schema">The optional database schema to use.</param>
        /// <returns>The <paramref name="configuration"/> instance to allow chaining.</returns>
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
