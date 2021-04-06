using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using YesSql.Sql;
using YesSql.Utils;

namespace YesSql.Provider.SqlServer
{
    public class SqlServerDialect : BaseDialect
    {
        private static readonly Dictionary<DbType, string> _columnTypes = new Dictionary<DbType, string>
        {
            {DbType.Guid, "UNIQUEIDENTIFIER"},
            {DbType.Binary, "VARBINARY(8000)"},
            {DbType.Time, "DATETIME"},
            {DbType.Date, "DATETIME"},
            {DbType.DateTime, "DATETIME" },
            {DbType.DateTime2, "DATETIME2" },
            {DbType.DateTimeOffset, "DATETIMEOFFSET" },
            {DbType.Boolean, "BIT"},
            {DbType.Byte, "TINYINT"},
            {DbType.SByte, "SMALLINT"},
            {DbType.Currency, "MONEY"},
            {DbType.Decimal, "DECIMAL({0},{1})"},
            {DbType.Double, "FLOAT(53)"},
            {DbType.Int16, "SMALLINT"},
            {DbType.UInt16, "SMALLINT"},
            {DbType.Int32, "INT"},
            {DbType.UInt32, "BIGINT"},
            {DbType.Int64, "BIGINT"},
            {DbType.UInt64, "NUMERIC(20)"},
            {DbType.Single, "REAL"},
            {DbType.AnsiStringFixedLength, "CHAR(1)"},
            {DbType.AnsiString, "VARCHAR(255)"},
            {DbType.StringFixedLength, "NCHAR(1)"},
            {DbType.String, "NVARCHAR(255)"},
        };

        static SqlServerDialect()
        {
            _propertyTypes = new Dictionary<Type, DbType>()
            {
                { typeof(object), DbType.Binary },
                { typeof(byte[]), DbType.Binary },
                { typeof(string), DbType.String },
                { typeof(char), DbType.StringFixedLength },
                { typeof(bool), DbType.Boolean },
                { typeof(byte), DbType.Byte },
                { typeof(sbyte), DbType.SByte }, // not supported
                { typeof(short), DbType.Int16 },
                { typeof(ushort), DbType.UInt16 }, // not supported
                { typeof(int), DbType.Int32 },
                { typeof(uint), DbType.UInt32 },
                { typeof(long), DbType.Int64 },
                { typeof(ulong), DbType.UInt64 },
                { typeof(float), DbType.Single },
                { typeof(double), DbType.Double },
                { typeof(decimal), DbType.Decimal },
                { typeof(DateTime), DbType.DateTime },
                { typeof(DateTimeOffset), DbType.DateTimeOffset },
                { typeof(Guid), DbType.Guid },
                { typeof(TimeSpan), DbType.Int64 },

                // Nullable types to prevent extra reflection on common ones
                { typeof(char?), DbType.StringFixedLength },
                { typeof(bool?), DbType.Boolean },
                { typeof(byte?), DbType.Byte },
                { typeof(sbyte?), DbType.Int16 },
                { typeof(short?), DbType.Int16 },
                { typeof(ushort?), DbType.UInt16 },
                { typeof(int?), DbType.Int32 },
                { typeof(uint?), DbType.UInt32 },
                { typeof(long?), DbType.Int64 },
                { typeof(ulong?), DbType.UInt64 },
                { typeof(float?), DbType.Single },
                { typeof(double?), DbType.Double },
                { typeof(decimal?), DbType.Decimal },
                { typeof(DateTime?), DbType.DateTime },
                { typeof(DateTimeOffset?), DbType.DateTimeOffset },
                { typeof(Guid?), DbType.Guid },
                { typeof(TimeSpan?), DbType.Int64 } // Mapping TimeSpan to Ticks
            };
        }

        public SqlServerDialect()
        {
            AddTypeHandler<TimeSpan, long>(x => x.Ticks);

            Methods.Add("second", new TemplateFunction("datepart(second, {0})"));
            Methods.Add("minute", new TemplateFunction("datepart(minute, {0})"));
            Methods.Add("hour", new TemplateFunction("datepart(hour, {0})"));
            Methods.Add("now", new TemplateFunction("getUtcDate()"));

            // These are not necessary since SQL Server 2008 
            //Methods.Add("day", new TemplateFunction("datepart(day, {0})"));
            //Methods.Add("month", new TemplateFunction("datepart(month, {0})"));
            //Methods.Add("year", new TemplateFunction("datepart(year, {0})"));
        }

        public override string Name => "SqlServer";
        public override string IdentitySelectString => "; select SCOPE_IDENTITY()";
        public override string IdentityLastId => "SCOPE_IDENTITY()";

        public override string RandomOrderByClause => "newid()";

        public override byte DefaultDecimalPrecision => 19;

        public override byte DefaultDecimalScale => 5;

        public override string GetTypeName(DbType dbType, int? length, byte? precision, byte? scale)
        {
            if (length.HasValue)
            {
                if (length.Value > 4000)
                {
                    if (dbType == DbType.String)
                    {
                        return "NVARCHAR(max)";
                    }

                    if (dbType == DbType.AnsiString || dbType == DbType.AnsiStringFixedLength)
                    {
                        return "VARCHAR(max)";
                    }

                    if (dbType == DbType.Binary)
                    {
                        return "VARBINARY(max)";
                    }
                }
                else
                {
                    if (dbType == DbType.String)
                    {
                        return $"NVARCHAR({length})";
                    }

                    if (dbType == DbType.AnsiString)
                    {
                        return $"VARCHAR({length})";
                    }

                    if (dbType == DbType.StringFixedLength)
                    {
                        return $"NCHAR({length})";
                    }

                    if (dbType == DbType.AnsiStringFixedLength)
                    {
                        return $"CHAR({length})";
                    }

                    if (dbType == DbType.Binary)
                    {
                        return $"VARBINARY({length})";
                    }
                }
            }

            if (_columnTypes.TryGetValue(dbType, out string value))
            {
                if (dbType == DbType.Decimal)
                {
                    value = string.Format(value, precision ?? DefaultDecimalPrecision, scale ?? DefaultDecimalScale);
                }

                return value;
            }

            throw new Exception("DbType not found for: " + dbType);
        }

        public override void Page(ISqlBuilder sqlBuilder, string offset, string limit)
        {
            if (offset != null)
            {
                sqlBuilder.Trail(" OFFSET ");
                sqlBuilder.Trail(offset);
                sqlBuilder.Trail(" ROWS");

                if (limit != null)
                {
                    sqlBuilder.Trail(" FETCH NEXT ");
                    sqlBuilder.Trail(limit);
                    sqlBuilder.Trail(" ROWS ONLY");
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

        public override string GetDropIndexString(string indexName, string tableName)
        {
            return "drop index if exists " + QuoteForColumnName(indexName) + " on " + QuoteForTableName(tableName);
        }

        public override string QuoteForColumnName(string columnName)
        {
            return "[" + columnName + "]";
        }

        public override string QuoteForTableName(string tableName)
        {
            return "[" + tableName + "]";
        }

        public override void Concat(IStringBuilder builder, params Action<IStringBuilder>[] generators)
        {
            builder.Append("(");

            for (var i = 0; i < generators.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(" + ");
                }

                generators[i](builder);
            }

            builder.Append(")");
        }

        public override string GetSqlValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value.GetType() == typeof(TimeSpan))
            {
                return ((TimeSpan)value).Ticks.ToString(CultureInfo.InvariantCulture);
            }

            return base.GetSqlValue(value);
        }

        public override bool SupportsIfExistsBeforeTableName => true;

        public override int MaxParametersPerCommand => 2098;
    }
}
