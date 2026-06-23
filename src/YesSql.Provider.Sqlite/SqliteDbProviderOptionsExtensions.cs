using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace YesSql.Provider.Sqlite
{
    /// <summary>
    /// Provides extension methods on <see cref="IConfiguration"/> to configure YesSql to use the SQLite provider.
    /// </summary>
    public static class SqliteDbProviderOptionsExtensions
    {
        /// <summary>
        /// Configures YesSql to use the SQLite document database provider.
        /// </summary>
        /// <param name="configuration">The configuration to register the provider on.</param>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        /// <returns>The <paramref name="configuration"/> instance to allow chaining.</returns>
        public static IConfiguration UseSqLite(this IConfiguration configuration, string connectionString)
            => UseSqLite(configuration, connectionString, IsolationLevel.Serializable);

        /// <summary>
        /// Configures YesSql to use the SQLite document database provider.
        /// </summary>
        /// <param name="configuration">The configuration to register the provider on.</param>
        /// <param name="connectionString">The connection string used to connect to the database.</param>
        /// <param name="isolationLevel">The isolation level used for transactions.</param>
        /// <returns>The <paramref name="configuration"/> instance to allow chaining.</returns>
        public static IConfiguration UseSqLite(this IConfiguration configuration, string connectionString, IsolationLevel isolationLevel)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            configuration.SqlDialect = new SqliteDialect();
            configuration.CommandInterpreter = new SqliteCommandInterpreter(configuration);
            configuration.ConnectionFactory = new DbConnectionFactory<SqliteConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
