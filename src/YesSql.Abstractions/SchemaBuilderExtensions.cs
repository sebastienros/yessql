using System;
using System.Collections.Generic;
using YesSql.Sql.Schema;

namespace YesSql.Sql
{
    public static class SchemaBuilderExtensions
    {
        public static IEnumerable<string> CreateSql(this ICommandInterpreter builder, ISchemaCommand command)
        {
            return builder.CreateSql(new[] { command });
        }

        public static ISchemaBuilder CreateReduceIndexTable<T>(this ISchemaBuilder builder, Action<ICreateTableCommand> table, string collection = null)
        {
            return builder.CreateReduceIndexTable(typeof(T), table, collection);
        }

        public static ISchemaBuilder DropReduceIndexTable<T>(this ISchemaBuilder builder, string collection = null)
        {
            return builder.DropReduceIndexTable(typeof(T), collection);
        }

        public static ISchemaBuilder CreateMapIndexTable<T>(this ISchemaBuilder builder, Action<ICreateTableCommand> table, string collection = null)
        {
            return builder.CreateMapIndexTable(typeof(T), table, collection);
        }

        public static ISchemaBuilder DropMapIndexTable<T>(this ISchemaBuilder builder, string collection = null)
        {
            return builder.DropMapIndexTable(typeof(T), collection);
        }

        public static ISchemaBuilder AlterIndexTable<T>(this ISchemaBuilder builder, Action<IAlterTableCommand> table, string collection = null)
        {
            return builder.AlterIndexTable(typeof(T), table, collection);
        }
    }
}
