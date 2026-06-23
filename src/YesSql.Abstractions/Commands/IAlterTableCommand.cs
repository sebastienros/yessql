using System;

namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a command that alters an existing table by adding, changing,
    /// renaming or removing columns and indexes.
    /// </summary>
    public interface IAlterTableCommand : ISchemaCommand
    {
        /// <summary>
        /// Adds a new column to the table.
        /// </summary>
        /// <param name="columnName">The name of the column to add.</param>
        /// <param name="dbType">The .NET type mapped to the database column type.</param>
        /// <param name="column">An optional delegate used to further configure the column.</param>
        void AddColumn(string columnName, Type dbType, Action<IAddColumnCommand> column = null);

        /// <summary>
        /// Adds a new column to the table.
        /// </summary>
        /// <typeparam name="T">The .NET type mapped to the database column type.</typeparam>
        /// <param name="columnName">The name of the column to add.</param>
        /// <param name="column">An optional delegate used to further configure the column.</param>
        void AddColumn<T>(string columnName, Action<IAddColumnCommand> column = null);

        /// <summary>
        /// Alters the definition of an existing column.
        /// </summary>
        /// <param name="columnName">The name of the column to alter.</param>
        /// <param name="column">An optional delegate used to configure the new column definition.</param>
        void AlterColumn(string columnName, Action<IAlterColumnCommand> column = null);

        /// <summary>
        /// Renames an existing column.
        /// </summary>
        /// <param name="columnName">The current name of the column.</param>
        /// <param name="newName">The new name of the column.</param>
        void RenameColumn(string columnName, string newName);

        /// <summary>
        /// Removes an existing column from the table.
        /// </summary>
        /// <param name="columnName">The name of the column to drop.</param>
        void DropColumn(string columnName);

        /// <summary>
        /// Creates an index on the table.
        /// </summary>
        /// <param name="indexName">The name of the index to create.</param>
        /// <param name="columnNames">The names of the columns the index is defined on.</param>
        void CreateIndex(string indexName, params string[] columnNames);

        /// <summary>
        /// Removes an existing index from the table.
        /// </summary>
        /// <param name="indexName">The name of the index to drop.</param>
        void DropIndex(string indexName);
    }
}
