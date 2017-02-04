using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace YesSql.Core.Sql.Providers.Sqlite
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

        public override string Name => "Sqlite";

        public override string IdentityColumnString => "integer primary key autoincrement";

        public override string IdentitySelectString => "select last_insert_rowid()";

        public override string GetTypeName(DbType dbType, int? length, byte precision, byte scale)
        {
            if (ColumnTypes.TryGetValue(dbType, out string value))
            {
                return value;
            }

            throw new Exception("DbType not found for: " + dbType);
        }

        public override void Page(SqlBuilder sqlBuilder, int offset, int limit)
        {
            var sb = new StringBuilder();

            sb.Append(" limit ");

            if (limit != 0)
            {
                sb.Append(limit);
            }

            if (offset != 0)
            {
                sb.Append(" offset ");
                sb.Append(offset);
            }

            sqlBuilder.Trail = sb.ToString();
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
