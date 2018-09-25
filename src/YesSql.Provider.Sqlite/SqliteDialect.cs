using System;
using System.Collections.Generic;
using System.Data;
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
            { DbType.AnsiString, "TEXT COLLATE NOCASE" },
            { DbType.String, "TEXT COLLATE NOCASE" },
            { DbType.AnsiStringFixedLength, "TEXT COLLATE NOCASE" },
            { DbType.StringFixedLength, "TEXT COLLATE NOCASE" },
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

        public override void Page(ISqlBuilder sqlBuilder, string offset, string limit)
        {
            sqlBuilder.ClearTrail();

            // If offset is defined without limit, use -1 as limit is mandatory on Sqlite
            if (offset != null && limit == null)
            {
                limit = "-1";
            }

            if (limit != null)
            {
                sqlBuilder.Trail(" LIMIT ");
                sqlBuilder.Trail(limit);
            }

            if (offset != null)
            {
                sqlBuilder.Trail(" OFFSET ");
                sqlBuilder.Trail(offset);
            }
        }

        public override string QuoteForColumnName(string columnName)
        {
            return "[" + columnName + "]";
        }

        public override string QuoteForTableName(string tableName)
        {
            return "[" + tableName + "]";
        }
    }
}
