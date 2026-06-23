namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a command that removes an existing foreign key constraint.
    /// </summary>
    public interface IDropForeignKeyCommand : ISchemaCommand
    {
        /// <summary>
        /// Gets the name of the source table that the foreign key is defined on.
        /// </summary>
        string SrcTable { get; }
    }
}
