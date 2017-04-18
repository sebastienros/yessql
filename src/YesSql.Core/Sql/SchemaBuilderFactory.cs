using System;
using System.Collections.Generic;
using System.Data;
using YesSql.Sql.Providers.MySql;
using YesSql.Sql.Providers.PostgreSql;
using YesSql.Sql.Providers.Sqlite;
using YesSql.Sql.Providers.SqlServer;

namespace YesSql.Sql
{
    public class SchemaBuilderFactory
    {
        private static readonly Dictionary<string, Func<ISqlDialect, ICommandInterpreter>> SchemaBuilders = new Dictionary<string, Func<ISqlDialect, ICommandInterpreter>>
        {
            {"sqliteconnection", d => new SqliteComandInterpreter(d)},
            {"sqlconnection", d => new SqlServerComandInterpreter(d)},
            {"mysqlconnection", d => new MySqlComandInterpreter(d)},
            {"npgsqlconnection", d => new PostgreSqlComandInterpreter(d)}

        };

        public static void RegisterSchemaBuilder(string connectionName, Func<ISqlDialect, ICommandInterpreter> schemaBuilder)
        {
            SchemaBuilders[connectionName] = schemaBuilder;
        }

        public static ICommandInterpreter For(IDbConnection connection)
        {
            string connectionName = connection.GetType().Name.ToLower();

            if (!SchemaBuilders.ContainsKey(connectionName))
            {
                throw new ArgumentException("Unknown connection name: " + connectionName);
            }

            var dialect = SqlDialectFactory.For(connection);

            return SchemaBuilders[connectionName](dialect);
        }
    }

}
