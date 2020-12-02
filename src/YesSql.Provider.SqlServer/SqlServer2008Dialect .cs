using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using YesSql.Sql;

namespace YesSql.Provider.SqlServer
{
    public class SqlServer2008Dialect : SqlServerDialect
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
            {DbType.UInt32, "BIGINT"},
            {DbType.Int64, "BIGINT"},
            {DbType.UInt64, "NUMERIC(20)"},
            {DbType.Single, "REAL"},
            {DbType.AnsiStringFixedLength, "CHAR(255)"},
            {DbType.AnsiString, "VARCHAR(255)"},
            {DbType.StringFixedLength, "NCHAR(255)"},
            {DbType.String, "NVARCHAR(255)"},
        };

        public SqlServer2008Dialect() : base()
        {
            // These are necessary for backward compatibility with SQL Server 2008 
            Methods.Add("day", new TemplateFunction("datepart(day, {0})"));
            Methods.Add("month", new TemplateFunction("datepart(month, {0})"));
            Methods.Add("year", new TemplateFunction("datepart(year, {0})"));
        }

        public override void Page(ISqlBuilder sqlBuilder, string offset, string limit)
        {
            if (offset != null)
            {
                var offsetVal = long.Parse(offset);
                sqlBuilder.InsertParentSelector("*");
                var sb = new StringBuilder();
                sb.Append(", ROW_NUMBER()");
                var currentOrder = sqlBuilder.GetOrder();
                if (currentOrder != "")
                {
                    sb.Append(" OVER( ORDER BY ");
                    sb.Append(currentOrder);
                    sb.Append(")");
                }
                sb.Append(" AS Seq");
                sqlBuilder.AddSelector(sb.ToString());
                if (limit != null)
                {
                    sqlBuilder.ParentWhereAnd($"Seq BETWEEN {offsetVal + 1} AND {long.Parse(limit) + offsetVal}");
                }
                else
                {
                    sqlBuilder.ParentWhereAnd($"Seq >= {offsetVal + 1}");
                }
            }
            else if (limit != null)
            {
                // Insert LIMIT clause after the select with brackets for parameters
                sqlBuilder.InsertSelector(" ");
                sqlBuilder.InsertSelector("(" + limit + ")");
                sqlBuilder.InsertSelector("TOP ");
            }
        }

    }
}
