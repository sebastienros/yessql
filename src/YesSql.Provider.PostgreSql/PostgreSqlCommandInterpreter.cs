using System;
using System.Data;
using System.Linq;
using System.Text;
using YesSql.Sql;
using YesSql.Sql.Schema;

namespace YesSql.Provider.PostgreSql
{
    /// <summary>
    /// Represents a command interpreter that generates SQL statements for PostgreSQL.
    /// </summary>
    public class PostgreSqlCommandInterpreter : BaseCommandInterpreter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlCommandInterpreter"/> class.
        /// </summary>
        /// <param name="configuration">The configuration used to generate SQL statements.</param>
        public PostgreSqlCommandInterpreter(IConfiguration configuration) : base(configuration)
        {
        }

        /// <summary>
        /// Appends the SQL statement that alters an existing column to the specified builder.
        /// </summary>
        /// <param name="builder">The builder to append the SQL statement to.</param>
        /// <param name="command">The alter column command to run.</param>
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

        /// <summary>
        /// Appends the SQL statement that adds an index to the specified builder, concurrently when the command requires it.
        /// </summary>
        /// <param name="builder">The builder to append the SQL statement to.</param>
        /// <param name="command">The add index command to run.</param>
        public override void Run(StringBuilder builder, IAddIndexCommand command)
        {
            if (!command.Concurrent)
            {
                base.Run(builder, command);
                return;
            }

            builder.AppendFormat("create index concurrently if not exists {1} on {0} ({2}) ",
                _dialect.QuoteForTableName(command.Name, _configuration.Schema),
                _dialect.QuoteForColumnName(command.IndexName),
                string.Join(", ", command.ColumnNames.Select(x => _dialect.QuoteForColumnName(GetRawColumnName(x))))
                );
        }
    }
}
