namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a command that removes an existing index from a table.
    /// </summary>
    public interface IDropIndexCommand : ITableCommand
    {
        /// <summary>
        /// Gets or sets the name of the index to drop.
        /// </summary>
        string IndexName { get; set; }
    }
}
