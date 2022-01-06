using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using YesSql.Sql;
using YesSql.Sql.Schema;

namespace YesSql.Provider.PostgreSql
{
    public class PostgreSqlCommandInterpreter : BaseCommandInterpreter
    {
        public PostgreSqlCommandInterpreter(ISqlDialect dialect) : base(dialect)
        {
        }

        public override void Run(StringBuilder builder, IAlterColumnCommand command)
        {
            builder.AppendFormat("alter table {2}{0} modify column {1} ",
                            _dialect.QuoteForTableName(command.Name),
                            _dialect.QuoteForColumnName(command.ColumnName),
                            _dialect.SchemaNameQuotedPrefix());
            var initLength = builder.Length;

            var dbType = _dialect.ToDbType(command.DbType);

            // type
            if (dbType != DbType.Object)
            {
                builder.Append(_dialect.GetTypeName(dbType, command.Length, command.Precision, command.Scale));
            }
            else
            {
                if (command.Length > 0 || command.Precision > 0 || command.Scale > 0)
                {
                    throw new Exception("Error while executing data migration: you need to specify the field's type in order to change its properties");
                }
            }

            // [default value]
            var builder2 = new StringBuilder();

            builder2.AppendFormat("alter table {0} alter column {1} ",
                            _dialect.QuoteForTableName(command.Name),
                            _dialect.QuoteForColumnName(command.ColumnName));
            var initLength2 = builder2.Length;

            if (command.Default != null)
            {
                builder2.Append(" set default ").Append(_dialect.GetSqlValue(command.Default)).Append(" ");
            }

            // result
            var result = new List<string>();

            if (builder.Length > initLength)
            {
                result.Add(builder.ToString());
            }

            if (builder2.Length > initLength2)
            {
                result.Add(builder2.ToString());
            }
        }
    }
}
