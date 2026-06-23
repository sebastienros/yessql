using MySqlConnector;
using System;
using System.Data;

namespace YesSql.Provider.MySql
{
    /// <summary>
    /// Provides extension methods on <see cref="IConfiguration"/> to configure YesSql to use the MySQL provider.
    /// </summary>
    public static class MySqlDbProviderOptionsExtensions
    {
        /// <summary>
        /// Configures YesSql to use the MySQL document database provider.
        /// </summary>
        /// <param name="configuration">The configuration to register the provider on.</param>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        /// <param name="schema">The optional database schema to use.</param>
        /// <returns>The <paramref name="configuration"/> instance to allow chaining.</returns>
        public static IConfiguration UseMySql(
            this IConfiguration configuration,
            string connectionString,
            string schema = null)
        {
            return UseMySql(configuration, connectionString, IsolationLevel.ReadUncommitted, schema);
        }

        /// <summary>
        /// Configures YesSql to use the MySQL document database provider.
        /// </summary>
        /// <param name="configuration">The configuration to register the provider on.</param>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        /// <param name="isolationLevel">The isolation level used for transactions.</param>
        /// <param name="schema">The optional database schema to use.</param>
        /// <returns>The <paramref name="configuration"/> instance to allow chaining.</returns>
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
