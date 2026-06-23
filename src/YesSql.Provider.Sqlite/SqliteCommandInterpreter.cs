using System.Collections.Generic;
using YesSql.Sql;
using YesSql.Sql.Schema;

namespace YesSql.Provider.Sqlite
{
    /// <summary>
    /// Represents a command interpreter that generates SQL statements for SQLite.
    /// </summary>
    public class SqliteCommandInterpreter : BaseCommandInterpreter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteCommandInterpreter"/> class.
        /// </summary>
        /// <param name="configuration">The configuration used to generate SQL statements.</param>
        public SqliteCommandInterpreter(IConfiguration configuration) : base(configuration)
        {
        }

        /// <summary>
        /// Generates the SQL statements for the specified create foreign key command.
        /// </summary>
        /// <param name="command">The create foreign key command to run.</param>
        /// <returns>An empty sequence, as SQLite does not support adding foreign keys to existing tables.</returns>
        public override IEnumerable<string> Run(ICreateForeignKeyCommand command)
        {
            yield break;
        }

        /// <summary>
        /// Generates the SQL statements for the specified drop foreign key command.
        /// </summary>
        /// <param name="command">The drop foreign key command to run.</param>
        /// <returns>An empty sequence, as SQLite does not support dropping foreign keys from existing tables.</returns>
        public override IEnumerable<string> Run(IDropForeignKeyCommand command)
        {
            yield break;
        }
    }
}
