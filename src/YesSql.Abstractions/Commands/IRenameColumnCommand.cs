namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a command that renames an existing column.
    /// </summary>
    public interface IRenameColumnCommand : IColumnCommand
    {
        /// <summary>
        /// Gets the new name of the column.
        /// </summary>
        string NewColumnName { get; }
    }
}
