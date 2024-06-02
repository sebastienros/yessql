using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using YesSql.Sql;
using YesSql.Sql.Schema;

namespace YesSql.Provider.MySql
{
    public class MySqlCommandInterpreter : BaseCommandInterpreter
    {
        private static readonly char[] Separators = { '(', ')', ' ' };

        public MySqlCommandInterpreter(IConfiguration configuration) : base(configuration)
        {
        }

        public override void Run(StringBuilder builder, IAlterColumnCommand command)
        {
            builder.AppendFormat("alter table {0} modify column {1} ",
                            _dialect.QuoteForTableName(command.Name, _configuration.Schema),
                            _dialect.QuoteForColumnName(command.ColumnName));
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
                            _dialect.QuoteForTableName(command.Name, _configuration.Schema),
                            _dialect.QuoteForColumnName(command.ColumnName));
            var initLength2 = builder2.Length;

            if (command.Default != null)
            {
                builder2.Append(" set default ").Append(_dialect.GetSqlValue(command.Default)).Append(' ');
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

        public override void Run(StringBuilder builder, IAddIndexCommand command)
        {
            builder.AppendFormat("create index {1} on {0} ({2}) ",
                _dialect.QuoteForTableName(command.Name, _configuration.Schema),
                _dialect.QuoteForColumnName(command.IndexName),
                string.Join(", ", command.ColumnNames.Select(x => GetColumnName(x)).ToArray())
                );
        }

        private string GetColumnName(string name)
        {
            var parts = name.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

            var final = _dialect.QuoteForColumnName(parts[0]);

            if (parts.Length > 1 && int.TryParse(parts[1], out var length))
            {
                final += $"({length})";
            }

            return final;
        }
    }
}
