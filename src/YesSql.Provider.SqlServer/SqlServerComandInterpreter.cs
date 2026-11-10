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

        /// <summary>
        /// Appends the SQL statement that adds an index to the specified builder, online when the command requires it.
        /// </summary>
        /// <remarks>
        /// Online index operations are only available on the Enterprise and Developer editions, Azure SQL Database and
        /// Azure SQL Managed Instance. The other editions create the index the regular way.
        /// </remarks>
        /// <param name="builder">The builder to append the SQL statement to.</param>
        /// <param name="command">The add index command to run.</param>
        public override void Run(StringBuilder builder, IAddIndexCommand command)
        {
            if (!command.Concurrent)
            {
                base.Run(builder, command);
                return;
            }

            var createIndex = new StringBuilder();
            base.Run(createIndex, command);
            var statement = createIndex.ToString().TrimEnd();

            // The online statement is run by EXEC so that the other editions don't reject the batch.
            // EngineEdition: 3 = Enterprise or Developer, 5 = Azure SQL Database, 8 = Azure SQL Managed Instance.
            builder
                .Append("if serverproperty('EngineEdition') in (3, 5, 8) exec(")
                .Append(_dialect.GetSqlValue(statement + " with (online = on)"))
                .Append(") else ")
                .Append(statement);
        }
    }
}
