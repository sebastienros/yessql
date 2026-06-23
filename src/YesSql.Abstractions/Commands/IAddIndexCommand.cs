namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a command that creates an index on one or more columns of a table.
    /// </summary>
    public interface IAddIndexCommand : ITableCommand
    {
        /// <summary>
        /// Gets or sets the name of the index to create.
        /// </summary>
        string IndexName { get; set; }

        /// <summary>
        /// Gets the names of the columns that the index is defined on.
        /// </summary>
        string[] ColumnNames { get; }
    }
}
