using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Sql.Schema;

namespace YesSql.Sql
{
    /// <summary>
    /// Provides extension methods for building database schemas using strongly typed index types.
    /// </summary>
    public static class SchemaBuilderExtensions
    {
        /// <summary>
        /// Generates the SQL statements for a single schema command.
        /// </summary>
        /// <param name="builder">The command interpreter that translates the command.</param>
        /// <param name="command">The schema command to translate.</param>
        /// <returns>The SQL statements that implement the command.</returns>
        public static IEnumerable<string> CreateSql(this ICommandInterpreter builder, ISchemaCommand command)
            => builder.CreateSql(new[] { command });

        /// <summary>
        /// Creates the table that backs the reduce index of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The reduce index type.</typeparam>
        /// <param name="builder">The schema builder.</param>
        /// <param name="table">A delegate used to configure the table columns.</param>
        /// <param name="collection">The name of the collection the index belongs to.</param>
        /// <returns>A task that completes when the table has been created.</returns>
        public static Task CreateReduceIndexTableAsync<T>(this ISchemaBuilder builder, Action<ICreateTableCommand> table, string collection = null)
            => builder.CreateReduceIndexTableAsync(typeof(T), table, collection);

        /// <summary>
        /// Drops the table that backs the reduce index of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The reduce index type.</typeparam>
        /// <param name="builder">The schema builder.</param>
        /// <param name="collection">The name of the collection the index belongs to.</param>
        /// <returns>A task that completes when the table has been dropped.</returns>
        public static Task DropReduceIndexTableAsync<T>(this ISchemaBuilder builder, string collection = null)
            => builder.DropReduceIndexTableAsync(typeof(T), collection);

        /// <summary>
        /// Creates the table that backs the map index of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The map index type.</typeparam>
        /// <param name="builder">The schema builder.</param>
        /// <param name="table">A delegate used to configure the table columns.</param>
        /// <param name="collection">The name of the collection the index belongs to.</param>
        /// <returns>A task that completes when the table has been created.</returns>
        public static Task CreateMapIndexTableAsync<T>(this ISchemaBuilder builder, Action<ICreateTableCommand> table, string collection = null)
            => builder.CreateMapIndexTableAsync(typeof(T), table, collection);

        /// <summary>
        /// Drops the table that backs the map index of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The map index type.</typeparam>
        /// <param name="builder">The schema builder.</param>
        /// <param name="collection">The name of the collection the index belongs to.</param>
        /// <returns>A task that completes when the table has been dropped.</returns>
        public static Task DropMapIndexTableAsync<T>(this ISchemaBuilder builder, string collection = null)
            => builder.DropMapIndexTableAsync(typeof(T), collection);

        /// <summary>
        /// Alters the table that backs the index of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The index type.</typeparam>
        /// <param name="builder">The schema builder.</param>
        /// <param name="table">A delegate used to configure the table changes.</param>
        /// <param name="collection">The name of the collection the index belongs to.</param>
        /// <returns>A task that completes when the table has been altered.</returns>
        public static Task AlterIndexTableAsync<T>(this ISchemaBuilder builder, Action<IAlterTableCommand> table, string collection = null)
            => builder.AlterIndexTableAsync(typeof(T), table, collection);
    }
}
