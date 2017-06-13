using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.Linq;
using System.Reflection;

namespace YesSql.Provider.Sqlite
{
    public static class SqliteDbProviderOptionsExtensions
    {
        public static IConfiguration UseInMemory(this IConfiguration configuration)
        {
            const string inMemoryConnectionString = "Data Source=:memory:";
            return UseSqLite(configuration, inMemoryConnectionString, IsolationLevel.Serializable, shareConnection: true);
        }

        public static IConfiguration UseSqLite(
            this IConfiguration configuration,
            string connectionString)
        {
            return UseSqLite(configuration, connectionString);
        }

        public static IConfiguration UseSqLite(
            this IConfiguration configuration,
            string connectionString,
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

            const string dialectParameterName = "Dialect";
            var dialectIndex = connectionString.IndexOf(dialectParameterName);
            var dialectTypeName = connectionString.Substring(dialectIndex + dialectParameterName.Length + 1);
            var dialectType = typeof(SqliteDialect).GetTypeInfo().Assembly
            .GetTypes().OfType<SqliteDialect>()
            .SingleOrDefault(d => d.Name == dialectTypeName);

            RegisterSqlite(connectionString, dialectType);
            configuration.ConnectionFactory = new DbConnectionFactory<SqliteConnection>(connectionString, shareConnection);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }

        private static void RegisterSqlite(string connectionString, SqliteDialect sqlServerDialect)
        {
            SqlDialectFactory.SqlDialects[connectionString] = sqlServerDialect;
            CommandInterpreterFactory.CommandInterpreters[connectionString] = d => new SqliteCommandInterpreter(d);
        }
    }
}
