namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a command that creates a foreign key constraint between two tables.
    /// </summary>
    public interface ICreateForeignKeyCommand : ISchemaCommand
    {
        /// <summary>
        /// Gets the names of the referenced columns in the destination table.
        /// </summary>
        string[] DestColumns { get; }

        /// <summary>
        /// Gets the name of the destination (referenced) table.
        /// </summary>
        string DestTable { get; }

        /// <summary>
        /// Gets the names of the columns in the source table that make up the foreign key.
        /// </summary>
        string[] SrcColumns { get; }

        /// <summary>
        /// Gets the name of the source (referencing) table.
        /// </summary>
        string SrcTable { get; }
    }
}
