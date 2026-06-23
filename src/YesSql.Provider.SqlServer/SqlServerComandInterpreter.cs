using System.Text;
using YesSql.Sql;
using YesSql.Sql.Schema;

namespace YesSql.Provider.SqlServer
{
    /// <summary>
    /// Represents a command interpreter that generates SQL statements for SQL Server.
    /// </summary>
    public class SqlServerCommandInterpreter : BaseCommandInterpreter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerCommandInterpreter"/> class.
        /// </summary>
        /// <param name="configuration">The configuration used to generate SQL statements.</param>
        public SqlServerCommandInterpreter(IConfiguration configuration) : base(configuration)
        {
        }

        /// <summary>
        /// Appends the SQL statement that renames an existing column to the specified builder.
        /// </summary>
        /// <param name="builder">The builder to append the SQL statement to.</param>
        /// <param name="command">The rename column command to run.</param>
        public override void Run(StringBuilder builder, IRenameColumnCommand command)
        {
            builder.AppendFormat("EXEC sp_RENAME {0}, {1}, 'COLUMN'",
                _dialect.GetSqlValue(_configuration.SqlDialect.QuoteForTableName(command.Name, _configuration.Schema) + "." + _configuration.SqlDialect.QuoteForColumnName(command.ColumnName)),
                // Don't [quote] the column name
                _dialect.GetSqlValue(command.NewColumnName)
                );            
        }
    }
}
