using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Sql.Schema;

namespace YesSql.Sql
{
    public static class SchemaBuilderExtensions
    {
        public static IEnumerable<string> CreateSql(this ICommandInterpreter builder, ISchemaCommand command)
            => builder.CreateSql(new[] { command });

        public static Task CreateReduceIndexTableAsync<T>(this ISchemaBuilder builder, Action<ICreateTableCommand> table, string collection = null)
            => builder.CreateReduceIndexTableAsync(typeof(T), table, collection);

        public static Task DropReduceIndexTableAsync<T>(this ISchemaBuilder builder, string collection = null)
            => builder.DropReduceIndexTableAsync(typeof(T), collection);

        public static Task CreateMapIndexTableAsync<T>(this ISchemaBuilder builder, Action<ICreateTableCommand> table, string collection = null)
            => builder.CreateMapIndexTableAsync(typeof(T), table, collection);

        public static Task DropMapIndexTableAsync<T>(this ISchemaBuilder builder, string collection = null)
            => builder.DropMapIndexTableAsync(typeof(T), collection);

        public static Task AlterIndexTableAsync<T>(this ISchemaBuilder builder, Action<IAlterTableCommand> table, string collection = null)
            => builder.AlterIndexTableAsync(typeof(T), table, collection);
    }
}
