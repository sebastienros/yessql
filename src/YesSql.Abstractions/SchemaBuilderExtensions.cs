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

        [Obsolete($"Instead, utilize the {nameof(CreateReduceIndexTableAsync)}<T> method. This current method is slated for removal in upcoming releases.")]
        public static ISchemaBuilder CreateReduceIndexTable<T>(this ISchemaBuilder builder, Action<ICreateTableCommand> table, string collection = null)
            => builder.CreateReduceIndexTable(typeof(T), table, collection);

        [Obsolete($"Instead, utilize the {nameof(DropReduceIndexTableAsync)}<T> method. This current method is slated for removal in upcoming releases.")]
        public static ISchemaBuilder DropReduceIndexTable<T>(this ISchemaBuilder builder, string collection = null)
            => builder.DropReduceIndexTable(typeof(T), collection);

        [Obsolete($"Instead, utilize the {nameof(CreateMapIndexTableAsync)}<T> method. This current method is slated for removal in upcoming releases.")]
        public static ISchemaBuilder CreateMapIndexTable<T>(this ISchemaBuilder builder, Action<ICreateTableCommand> table, string collection = null)
            => builder.CreateMapIndexTable(typeof(T), table, collection);

        [Obsolete($"Instead, utilize the {nameof(DropMapIndexTableAsync)}<T> method. This current method is slated for removal in upcoming releases.")]
        public static ISchemaBuilder DropMapIndexTable<T>(this ISchemaBuilder builder, string collection = null)
            => builder.DropMapIndexTable(typeof(T), collection);

        [Obsolete($"Instead, utilize the {nameof(AlterIndexTableAsync)}<T> method. This current method is slated for removal in upcoming releases.")]
        public static ISchemaBuilder AlterIndexTable<T>(this ISchemaBuilder builder, Action<IAlterTableCommand> table, string collection = null)
            => builder.AlterIndexTable(typeof(T), table, collection);

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
