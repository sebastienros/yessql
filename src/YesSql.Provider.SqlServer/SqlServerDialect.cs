using System;
using System.Collections.Generic;
using System.Data;
using YesSql.Sql;

namespace YesSql.Provider.SqlServer
{
    public class SqlServerDialect : BaseDialect
    {
        private static Dictionary<DbType, string> ColumnTypes = new Dictionary<DbType, string>
        {
            {DbType.Guid, "UNIQUEIDENTIFIER"},
            {DbType.Binary, "VARBINARY(8000)"},
            {DbType.Time, "DATETIME"},
            {DbType.Date, "DATETIME"},
            {DbType.DateTime, "DATETIME" },
            {DbType.DateTime2, "DATETIME2" },
            {DbType.DateTimeOffset, "datetimeoffset" },
            {DbType.Boolean, "BIT"},
            {DbType.Byte, "TINYINT"},
            {DbType.Currency, "MONEY"},
            {DbType.Decimal, "DECIMAL(19,5)"},
            {DbType.Double, "FLOAT(53)"},
            {DbType.Int16, "SMALLINT"},
            {DbType.UInt16, "SMALLINT"},
            {DbType.Int32, "INT"},
            {DbType.UInt32, "INT"},
            {DbType.Int64, "BIGINT"},
            {DbType.UInt64, "BIGINT"},
            {DbType.Single, "REAL"},
            {DbType.AnsiStringFixedLength, "CHAR(255)"},
            {DbType.AnsiString, "VARCHAR(255)"},
            {DbType.StringFixedLength, "NCHAR(255)"},
            {DbType.String, "NVARCHAR(255)"},
        };

        public SqlServerDialect()
        {
            Methods.Add("second", new TemplateFunction("datepart(second, {0})"));
            Methods.Add("minute", new TemplateFunction("datepart(minute, {0})"));
            Methods.Add("hour", new TemplateFunction("datepart(hour, {0})"));

            // These are not necessary since SQL Server 2008 
            //Methods.Add("day", new TemplateFunction("datepart(day, {0})"));
            //Methods.Add("month", new TemplateFunction("datepart(month, {0})"));
            //Methods.Add("year", new TemplateFunction("datepart(year, {0})"));
        }

        public override string Name => "SqlServer";
        public override string IdentitySelectString => "; select SCOPE_IDENTITY()";

        public override ISqlBuilder CreateBuilder(string tablePrefix)
        {
            return new SqlServerSqlBuilder(tablePrefix, this);
        }

        public override string GetTypeName(DbType dbType, int? length, byte precision, byte scale)
        {
            if (length.HasValue)
            {
                if (length.Value > 4000)
                {
                    if (dbType == DbType.String)
                    {
                        return "NTEXT";
                    }

                    if (dbType == DbType.AnsiString)
                    {
                        return "TEXT";
                    }

                    if (dbType == DbType.Binary)
                    {
                        return "BLOB";
                    }
                }
                else
                {
                    if (dbType == DbType.String)
                    {
                        return "NVARCHAR(" + length + ")";
                    }

                    if (dbType == DbType.AnsiString)
                    {
                        return "VARCHAR(" + length + ")";
                    }

                    if (dbType == DbType.Binary)
                    {
                        return "VARBINARY(" + length + ")";
                    }
                }
            }

            if (ColumnTypes.TryGetValue(dbType, out string value))
            {
                return value;
            }

            throw new Exception("DbType not found for: " + dbType);
        }

        public override void Page(ISqlBuilder sqlBuilder, int offset, int limit)
        {
            if (offset == 0 && limit != 0)
            {
                // Insert LIMIT clause after the select
                var selector = sqlBuilder.GetSelector();
                selector = " TOP " + limit + " " + selector;
                sqlBuilder.Selector(selector);
            }
            else if (offset != 0 || limit != 0)
            {
                sqlBuilder.Trail = "OFFSET " + offset + " ROWS FETCH FIRST " + limit + " ROWS ONLY";
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

        protected override string Quote(string value)
        {
            return QuoteString + value.Replace(QuoteString, DoubleQuoteString) + QuoteString;
        }
    }
}
