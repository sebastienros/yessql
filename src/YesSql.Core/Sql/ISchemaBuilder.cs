using System;
using System.Collections.Generic;
using System.Data.Common;
using YesSql.Core.Sql.Schema;
using YesSql.Core.Sql.SchemaBuilders;

namespace YesSql.Core.Sql
{
    public interface ISchemaBuilder
    {
        IEnumerable<string> CreateSql(IEnumerable<ISchemaBuilderCommand> commands);
    }
    public static class SchemaBuilderExtensions
    {
        public static IEnumerable<string> CreateSql(this ISchemaBuilder builder, ISchemaBuilderCommand command)
        {
            return builder.CreateSql(new[] { command });
        }
    }

    public class SchemaBuilderFactory
    {
        private static readonly Dictionary<string, Func<ISqlDialect, ISchemaBuilder>> SchemaBuilders = new Dictionary<string, Func<ISqlDialect, ISchemaBuilder>>
        {
            {"sqliteconnection", d => new SqliteSchemaBuilder(d)},
            {"sqlconnection", d => new SqlServerSchemaBuilder(d)},
            {"mysqlconnection", d => new MySqlSchemaBuilder(d)}

        };

        public static void RegisterSchemaBuilder(string connectionName, Func<ISqlDialect, ISchemaBuilder> schemaBuilder)
        {
            SchemaBuilders[connectionName] = schemaBuilder;
        }

        public static ISchemaBuilder For(DbConnection connection)
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
