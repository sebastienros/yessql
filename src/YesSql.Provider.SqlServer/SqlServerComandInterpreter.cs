using System.Text;
using YesSql.Sql;
using YesSql.Sql.Schema;

namespace YesSql.Provider.SqlServer
{
    public class SqlServerCommandInterpreter : BaseCommandInterpreter
    {
        public SqlServerCommandInterpreter(IConfiguration configuration) : base(configuration)
        {
        }

        public override void Run(StringBuilder builder, IRenameColumnCommand command) 
            => builder.AppendFormat("EXEC sp_RENAME {0}, {1}, 'COLUMN'",
                _dialect.GetSqlValue(_configuration.SqlDialect.QuoteForTableName(command.Name, _configuration.Schema) + "." + _configuration.SqlDialect.QuoteForColumnName(command.ColumnName)),
                // Don't [quote] the column name
                _dialect.GetSqlValue(command.NewColumnName)
                );
    }
}
