using System.Collections.Generic;

namespace YesSql.Sql.Schema
{
    /// <summary>
    /// Represents a command that executes a raw SQL statement against the database.
    /// </summary>
    public interface ISqlStatementCommand : ISchemaCommand
    {
        /// <summary>
        /// Gets the raw SQL statement to execute.
        /// </summary>
        string Sql { get; }

        /// <summary>
        /// Gets the list of database providers the statement applies to. An empty list means it applies to all providers.
        /// </summary>
        List<string> Providers { get; }
    }
}
