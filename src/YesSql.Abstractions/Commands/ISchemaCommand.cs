using System.Collections.Generic;

namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a schema modification command, such as creating, altering or dropping a table.
    /// </summary>
    public interface ISchemaCommand
    {
        /// <summary>
        /// Gets the name of the schema object the command operates on.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the list of table commands that make up this schema command.
        /// </summary>
        List<ITableCommand> TableCommands { get; }
    }

    /// <summary>
    /// Defines the types of schema commands that can be issued.
    /// </summary>
    public enum SchemaCommandType
    {
        /// <summary>
        /// A command that creates a table.
        /// </summary>
        CreateTable,

        /// <summary>
        /// A command that drops a table.
        /// </summary>
        DropTable,

        /// <summary>
        /// A command that alters an existing table.
        /// </summary>
        AlterTable,

        /// <summary>
        /// A command that executes a raw SQL statement.
        /// </summary>
        SqlStatement,

        /// <summary>
        /// A command that creates a foreign key constraint.
        /// </summary>
        CreateForeignKey,

        /// <summary>
        /// A command that drops a foreign key constraint.
        /// </summary>
        DropForeignKey,

        /// <summary>
        /// A command that creates a database schema.
        /// </summary>
        CreateSchema,
    }
}
