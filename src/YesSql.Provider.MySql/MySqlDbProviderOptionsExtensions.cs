using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Reflection;

namespace YesSql.Provider.MySql
{
    public static class MySqlDbProviderOptionsExtensions
    {
        public static IConfiguration UseMySql(
            this IConfiguration configuration,
            string connectionString)
        {
            return UseMySql(configuration, connectionString, IsolationLevel.ReadUncommitted);
        }

        public static IConfiguration UseMySql(
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
            var dialectType = typeof(MySqlDialect).GetTypeInfo().Assembly
                .GetTypes().OfType<MySqlDialect>()
                .SingleOrDefault(d => d.Name == dialectTypeName);

            RegisterMySqlServer(connectionString, dialectType);
            configuration.ConnectionFactory = new DbConnectionFactory<MySqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }

        private static void RegisterMySqlServer(string connectionString, MySqlDialect sqlServerDialect)
        {
            SqlDialectFactory.SqlDialects[connectionString] = sqlServerDialect;
            CommandInterpreterFactory.CommandInterpreters[connectionString] = d => new MySqlCommandInterpreter(d);
        }
    }
}
