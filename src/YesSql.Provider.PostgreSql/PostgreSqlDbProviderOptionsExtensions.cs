using Npgsql;
using System;
using System.Data;

namespace YesSql.Provider.PostgreSql
{
    /// <summary>
    /// Provides extension methods on <see cref="IConfiguration"/> to configure YesSql to use the PostgreSQL provider.
    /// </summary>
    public static class PostgreSqlDbProviderOptionsExtensions
    {
        /// <summary>
        /// Configures YesSql to use the PostgreSQL document database provider.
        /// </summary>
        /// <param name="configuration">The configuration to register the provider on.</param>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        /// <param name="schema">The optional database schema to use.</param>
        /// <returns>The <paramref name="configuration"/> instance to allow chaining.</returns>
        public static IConfiguration UsePostgreSql(this IConfiguration configuration, string connectionString, string schema = null)
            => UsePostgreSql(configuration, connectionString, IsolationLevel.ReadUncommitted, schema);

        /// <summary>
        /// Configures YesSql to use the PostgreSQL document database provider.
        /// </summary>
        /// <param name="configuration">The configuration to register the provider on.</param>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        /// <param name="isolationLevel">The isolation level used for transactions.</param>
        /// <param name="schema">The optional database schema to use.</param>
        /// <returns>The <paramref name="configuration"/> instance to allow chaining.</returns>
        public static IConfiguration UsePostgreSql(this IConfiguration configuration, string connectionString, IsolationLevel isolationLevel, string schema = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            configuration.SqlDialect = new PostgreSqlDialect();
            configuration.Schema = schema;
            configuration.CommandInterpreter = new PostgreSqlCommandInterpreter(configuration);
            configuration.ConnectionFactory = new DbConnectionFactory<NpgsqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
