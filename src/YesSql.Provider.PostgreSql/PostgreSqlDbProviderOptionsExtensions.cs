using Npgsql;
using System;
using System.Data;
using System.Linq;
using System.Reflection;

namespace YesSql.Provider.PostgreSql
{
    public static class PostgreSqlDbProviderOptionsExtensions
    {
        public static IConfiguration UsePostgreSql(
            this IConfiguration configuration,
            string connectionString)
        {
            return UsePostgreSql(configuration, connectionString, IsolationLevel.ReadUncommitted);
        }

        public static IConfiguration UsePostgreSql(
            this IConfiguration configuration,
            string connectionString,
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

            const string dialectParameterName = "Dialect";
            var dialectIndex = connectionString.IndexOf(dialectParameterName);
            var dialectTypeName = connectionString.Substring(dialectIndex + dialectParameterName.Length + 1);
            var dialectType = typeof(PostgreSqlDialect).GetTypeInfo().Assembly
                .GetTypes().OfType<PostgreSqlDialect>()
                .SingleOrDefault(d => d.Name == dialectTypeName);

            RegisterPostgreSql(connectionString, dialectType);
            configuration.ConnectionFactory = new DbConnectionFactory<NpgsqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }

        private static void RegisterPostgreSql(string connectionString, PostgreSqlDialect sqlServerDialect)
        {
            SqlDialectFactory.SqlDialects[connectionString] = sqlServerDialect;
            CommandInterpreterFactory.CommandInterpreters[connectionString] = d => new PostgreSqlCommandInterpreter(d);
        }
    }
}
