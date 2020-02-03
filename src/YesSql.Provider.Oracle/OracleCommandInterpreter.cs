using System;
using System.Data;
using System.Text;
using YesSql.Sql;
using YesSql.Sql.Schema;

namespace YesSql.Provider.Oracle
{
    public class OracleCommandInterpreter : BaseCommandInterpreter
    {
        public OracleCommandInterpreter(ISqlDialect dialect) : base(dialect) { }

        public override void Run(StringBuilder builder, IAlterColumnCommand command)
        {
            builder.AppendFormat("alter table {0} modify {1} ",
                            _dialect.QuoteForTableName(command.Name),
                            _dialect.QuoteForColumnName(command.ColumnName));
            // type
            if (command.DbType != DbType.Object)
            {
                builder.Append(_dialect.GetTypeName(command.DbType, command.Length, command.Precision, command.Scale));
            }
            else
            {
                if (command.Length > 0 || command.Precision > 0 || command.Scale > 0)
                {
                    throw new Exception("Error while executing data migration: you need to specify the field's type in order to change its properties");
                }
            }

            if (command.Default != null)
            {
                builder.Append(" default ").Append(_dialect.GetSqlValue(command.Default)).Append(" ");
            }
        }
    }
}
