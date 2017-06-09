using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace YesSql.Provider.Sqlite
{
    public static class SqliteDbProviderOptionsExtensions
    {
        public static readonly SqliteDialect DefaulSqliteDialect = new SqliteDialect();

        public static IConfiguration RegisterSqLite(this IConfiguration configuration)
        {
            return RegisterSqLite(configuration, DefaulSqliteDialect);
        }

        public static IConfiguration RegisterSqLite(this IConfiguration configuration, SqliteDialect sqliteDialect)
        {
            SqlDialectFactory.SqlDialects["sqliteconnection"] = sqliteDialect;
            CommandInterpreterFactory.CommandInterpreters["sqliteconnection"] = d => new SqliteCommandInterpreter(d);

            return configuration;
        }

        public static IConfiguration UseInMemory(this IConfiguration configuration)
        {
            const string inMemoryConnectionString = "Data Source=:memory:";
            return UseSqLite(configuration, inMemoryConnectionString, DefaulSqliteDialect, IsolationLevel.Serializable, shareConnection: true);
        }

        public static IConfiguration UseSqLite(
            this IConfiguration configuration,
            string connectionString)
        {
            return UseSqLite(configuration, connectionString, DefaulSqliteDialect);
        }

        public static IConfiguration UseSqLite(
            this IConfiguration configuration,
            string connectionString,
            SqliteDialect sqliteDialect)
        {
            return UseSqLite(configuration, connectionString, sqliteDialect, IsolationLevel.Serializable);
        }

        public static IConfiguration UseSqLite(
           this IConfiguration configuration,
           string connectionString,
           IsolationLevel isolationLevel)
        {
            return UseSqLite(configuration, connectionString, DefaulSqliteDialect, isolationLevel);
        }

        public static IConfiguration UseSqLite(
            this IConfiguration configuration,
            string connectionString,
            SqliteDialect sqliteDialect,
            IsolationLevel isolationLevel,
            bool shareConnection = false)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            if (sqliteDialect == null)
            {
                throw new ArgumentNullException(nameof(sqliteDialect));
            }

            RegisterSqLite(configuration, sqliteDialect);
            configuration.ConnectionFactory = new DbConnectionFactory<SqliteConnection>(connectionString, shareConnection);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
