using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace YesSql.Provider.SqlServer
{
    public static class SqlServerDbProviderOptionsExtensions
    {
        public static IConfiguration UseSqlServer(
            this IConfiguration configuration,
            string connectionString)
        {
            return UseSqlServer(configuration, connectionString, IsolationLevel.ReadUncommitted);
        }

        public static IConfiguration UseSqlServer(
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
            var dialectType = typeof(SqlServerDialect).GetTypeInfo().Assembly
                .GetTypes().OfType<SqlServerDialect>()
                .SingleOrDefault(d => d.Name == dialectTypeName);

            RegisterSqlServer(connectionString, dialectType);
            configuration.ConnectionFactory = new DbConnectionFactory<SqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }

        private static void RegisterSqlServer(string connectionString, SqlServerDialect sqlServerDialect)
        {
            SqlDialectFactory.SqlDialects[connectionString] = sqlServerDialect;
            CommandInterpreterFactory.CommandInterpreters[connectionString] = d => new SqlServerCommandInterpreter(d);
        }
    }
}
