using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace YesSql.Provider.MySql
{
    public static class MySqlDbProviderOptionsExtensions
    {
        public static readonly MySqlDialect DefaulMySqlDialect = new MySqlDialect();

        public static IConfiguration RegisterMySql(this IConfiguration configuration)
        {
            return RegisterMySql(configuration, DefaulMySqlDialect);
        }

        public static IConfiguration RegisterMySql(this IConfiguration configuration, MySqlDialect mySqlDialect)
        {
            SqlDialectFactory.SqlDialects["mysqlconnection"] = mySqlDialect;
            CommandInterpreterFactory.CommandInterpreters["mysqlconnection"] = d => new MySqlCommandInterpreter(d);

            return configuration;
        }

        public static IConfiguration UseMySql(
            this IConfiguration configuration,
            string connectionString)
        {
            return UseMySql(configuration, connectionString, DefaulMySqlDialect, IsolationLevel.ReadUncommitted);
        }

        public static IConfiguration UseMySql(
            this IConfiguration configuration,
            string connectionString,
            MySqlDialect mySqlDialect)
        {
            return UseMySql(configuration, connectionString, mySqlDialect, IsolationLevel.ReadUncommitted);
        }

        public static IConfiguration UseMySql(
            this IConfiguration configuration,
            string connectionString,
            IsolationLevel isolationLevel)
        {
            return UseMySql(configuration, connectionString, DefaulMySqlDialect, isolationLevel);
        }

        public static IConfiguration UseMySql(
            this IConfiguration configuration,
            string connectionString,
            MySqlDialect mySqlDialect,
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

            if (mySqlDialect == null)
            {
                throw new ArgumentNullException(nameof(mySqlDialect));
            }

            RegisterMySql(configuration, mySqlDialect);

            configuration.ConnectionFactory = new DbConnectionFactory<MySqlConnection>(connectionString);
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}
