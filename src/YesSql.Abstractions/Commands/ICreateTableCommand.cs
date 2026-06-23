using System;

namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a command that creates a new table.
    /// </summary>
    public interface ICreateTableCommand : ISchemaCommand
    {
        /// <summary>
        /// Adds a column to the table being created.
        /// </summary>
        /// <param name="columnName">The name of the column to add.</param>
        /// <param name="dbType">The .NET type mapped to the database column type.</param>
        /// <param name="column">An optional delegate used to further configure the column.</param>
        /// <returns>The current <see cref="ICreateTableCommand"/> instance so that calls can be chained.</returns>
        ICreateTableCommand Column(string columnName, Type dbType, Action<ICreateColumnCommand> column = null);

        /// <summary>
        /// Adds a column to the table being created.
        /// </summary>
        /// <typeparam name="T">The .NET type mapped to the database column type.</typeparam>
        /// <param name="columnName">The name of the column to add.</param>
        /// <param name="column">An optional delegate used to further configure the column.</param>
        /// <returns>The current <see cref="ICreateTableCommand"/> instance so that calls can be chained.</returns>
        ICreateTableCommand Column<T>(string columnName, Action<ICreateColumnCommand> column = null);

        /// <summary>
        /// Adds an identity column to the table being created, using the type matching the specified size.
        /// </summary>
        /// <param name="identityColumnSize">The size of the identity column, which determines whether an <see cref="int"/> or a <see cref="long"/> column is created.</param>
        /// <param name="columnName">The name of the column to add.</param>
        /// <param name="column">An optional delegate used to further configure the column.</param>
        /// <returns>The current <see cref="ICreateTableCommand"/> instance so that calls can be chained.</returns>
        public ICreateTableCommand Column(IdentityColumnSize identityColumnSize, string columnName, Action<ICreateColumnCommand> column = null) => identityColumnSize == IdentityColumnSize.Int32 ? Column<int>(columnName, column) : Column<long>(columnName, column);
    }
}
