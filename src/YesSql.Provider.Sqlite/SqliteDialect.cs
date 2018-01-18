using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using YesSql.Sql;

namespace YesSql.Provider.Sqlite
{
    public class SqliteDialect : BaseDialect
    {
        private static Dictionary<DbType, string> ColumnTypes = new Dictionary<DbType, string>
        {
            { DbType.Binary, "BLOB" },
            { DbType.Byte, "TINYINT" },
            { DbType.Int16, "SMALLINT" },
            { DbType.Int32, "INT" },
            { DbType.Int64, "BIGINT" },
            { DbType.SByte, "INTEGER" },
            { DbType.UInt16, "INTEGER" },
            { DbType.UInt32, "INTEGER" },
            { DbType.UInt64, "INTEGER" },
            { DbType.Currency, "NUMERIC" },
            { DbType.Decimal, "NUMERIC" },
            { DbType.Double, "DOUBLE" },
            { DbType.Single, "DOUBLE" },
            { DbType.VarNumeric, "NUMERIC" },
            { DbType.AnsiString, "TEXT" },
            { DbType.String, "TEXT" },
            { DbType.AnsiStringFixedLength, "TEXT" },
            { DbType.StringFixedLength, "TEXT" },
            { DbType.Date, "DATE" },
            { DbType.DateTime, "DATETIME" },
            { DbType.DateTime2, "DATETIME" },
            { DbType.DateTimeOffset, "DATETIME" },
            { DbType.Time, "TIME" },
            { DbType.Boolean, "BOOL" },
            { DbType.Guid, "UNIQUEIDENTIFIER" }
        };

        public SqliteDialect()
        {
            Methods.Add("second", new TemplateFunction("cast(strftime('%S', {0}) as int)"));
            Methods.Add("minute", new TemplateFunction("cast(strftime('%M', {0}) as int)"));
            Methods.Add("hour", new TemplateFunction("cast(strftime('%H', {0}) as int)"));
            Methods.Add("day", new TemplateFunction("cast(strftime('%d', {0}) as int)"));
            Methods.Add("month", new TemplateFunction("cast(strftime('%m', {0}) as int)"));
            Methods.Add("year", new TemplateFunction("cast(strftime('%Y', {0}) as int)"));
        }

        public override string Name => "Sqlite";

        public override string IdentityColumnString => "integer primary key autoincrement";

        public override string IdentitySelectString => "; select last_insert_rowid()";

        public override string GetTypeName(DbType dbType, int? length, byte precision, byte scale)
        {
            if (ColumnTypes.TryGetValue(dbType, out string value))
            {
                return value;
            }

            throw new Exception("DbType not found for: " + dbType);
        }

        public override void Page(ISqlBuilder sqlBuilder, int offset, int limit)
        {
            sqlBuilder.ClearTrail();
            if (limit != 0)
            {
                sqlBuilder.Trail(" LIMIT ");
                sqlBuilder.Trail(limit.ToString());
            }

            if (offset != 0)
            {
                sqlBuilder.Trail(" OFFSET ");
                sqlBuilder.Trail(offset.ToString());
            }
        }

        protected override string Quote(string value)
        {
            return SingleQuoteString + value.Replace(SingleQuoteString, DoubleSingleQuoteString) + SingleQuoteString;
        }

        public override string QuoteForColumnName(string columnName)
        {
            return QuoteString + columnName.Replace(QuoteString, DoubleQuoteString) + QuoteString;
        }

        public override string QuoteForTableName(string tableName)
        {
            return QuoteString + tableName.Replace(QuoteString, DoubleQuoteString) + QuoteString;
        }

        public override ISqlBuilder CreateBuilder(string tablePrefix)
        {
            return new SqliteSqlBuilder(tablePrefix, this);
        }
    }
}
