using System;
using System.Data;
using System.Text;
using YesSql.Sql;
using YesSql.Sql.Schema;

namespace YesSql.Provider.PostgreSql
{
    public class PostgreSqlCommandInterpreter : BaseCommandInterpreter
    {
        public PostgreSqlCommandInterpreter(IConfiguration configuration) : base(configuration)
        {
        }

        public override void Run(StringBuilder builder, IAlterColumnCommand command)
        {
            builder.AppendFormat("alter table {0} alter column {1} ",
                _dialect.QuoteForTableName(command.Name, _configuration.Schema),
                _dialect.QuoteForColumnName(command.ColumnName));

            var dbType = _dialect.ToDbType(command.DbType);

            if (dbType != DbType.Object)
            {
                builder
                    .Append("type ")
                    .Append(_dialect.GetTypeName(dbType, command.Length, command.Precision, command.Scale));
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
                builder
                    .Append(" set default ")
                    .Append(_dialect.GetSqlValue(command.Default));
            }
        }
    }
}
