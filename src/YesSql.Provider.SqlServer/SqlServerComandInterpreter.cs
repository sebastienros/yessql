using System.Text;
using YesSql.Sql;
using YesSql.Sql.Schema;

namespace YesSql.Provider.SqlServer
{
    public class SqlServerCommandInterpreter : BaseCommandInterpreter
    {
        public SqlServerCommandInterpreter(ISqlDialect dialect) : base(dialect)
        {
        }

        public override void Run(StringBuilder builder, IRenameColumnCommand command)
        {
            builder.AppendFormat("EXEC sp_RENAME {0}, {1}, 'COLUMN'",
                _dialect.GetSqlValue(command.Name + "." + command.ColumnName),
                _dialect.GetSqlValue(command.NewColumnName)
                );            
        }
    }
}
