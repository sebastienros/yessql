using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using YesSql.Sql;

namespace YesSql.Provider.PostgreSql
{
    public class PostgreSqlDialect : BaseDialect
    {
        private static readonly Dictionary<DbType, string> _columnTypes = new Dictionary<DbType, string>
        {
            {DbType.Guid, "char(36)"},
            {DbType.Binary, "bytea"},
            {DbType.Date, "date"},
            {DbType.Time, "time"},
            {DbType.DateTime, "timestamp" },
            {DbType.DateTime2, "timestamp" },
            {DbType.DateTimeOffset, "varchar(255)" }, // should not be reached since DateTimeOffset is converted to strings
            {DbType.Boolean, "boolean"},
            {DbType.Byte, "int2"},
            {DbType.SByte, "int2"},
            {DbType.Decimal, "decimal({0}, {1})"},
            {DbType.Single, "float4"},
            {DbType.Double, "float8"},
            {DbType.Int16, "int2"},
            {DbType.Int32, "int4"},
            {DbType.Int64, "int8"},
            {DbType.UInt16, "int2"},
            {DbType.UInt32, "int4"},
            {DbType.UInt64, "int8"},
            {DbType.AnsiStringFixedLength, "char(1)"},
            {DbType.AnsiString, "varchar(255)"},
            {DbType.StringFixedLength, "char(1)"},
            {DbType.String, "varchar(255)"},
            {DbType.Currency, "decimal(16,4)"}
        };

        static PostgreSqlDialect()
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
                { typeof(DateTimeOffset), DbType.String },
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
                { typeof(DateTimeOffset?), DbType.String },
                { typeof(Guid?), DbType.Guid },
                { typeof(TimeSpan?), DbType.Int64 }
            };
        }

        public PostgreSqlDialect()
        {
            AddTypeHandler<TimeSpan, long>(x => x.Ticks);

            Methods.Add("second", new TemplateFunction("extract(second from {0})"));
            Methods.Add("minute", new TemplateFunction("extract(minute from {0})"));
            Methods.Add("hour", new TemplateFunction("extract(hour from {0})"));
            Methods.Add("day", new TemplateFunction("extract(day from {0})"));
            Methods.Add("month", new TemplateFunction("extract(month from {0})"));
            Methods.Add("year", new TemplateFunction("extract(year from {0})"));
        }

        public override string Name => "PostgreSql";
        public override string InOperator(string values) => " = any(array[" + values + "])";
        public override string NotInOperator(string values) => " <> all(array[" + values + "])";
        public override string IdentitySelectString => "RETURNING";
        public override string IdentityLastId => $"lastval()";
        public override string IdentityColumnString => "SERIAL PRIMARY KEY";
        public override string RandomOrderByClause => "random()";
        public override bool SupportsIfExistsBeforeTableName => true;
        public override bool PrefixIndex => true;

        public override string GetTypeName(DbType dbType, int? length, byte? precision, byte? scale)
        {
            if (length.HasValue)
            {
                if (length.Value > 4000)
                {
                    if (dbType == DbType.String)
                    {
                        return "text";
                    }

                    if (dbType == DbType.AnsiString)
                    {
                        return "text";
                    }
                }
                else
                {
                    if (dbType == DbType.String)
                    {
                        return "varchar(" + length + ")";
                    }

                    if (dbType == DbType.AnsiString)
                    {
                        return "varchar(" + length + ")";
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

        public override string GetDropForeignKeyConstraintString(string name)
        {
            return " drop foreign key " + name;
        }

        public override string DefaultValuesInsert => "DEFAULT VALUES";

        public override void Page(ISqlBuilder sqlBuilder, string offset, string limit)
        {
            sqlBuilder.Trail(" limit ");

            if (offset != null && limit == null)
            {
                sqlBuilder.Trail(" all");
            }

            if (limit != null)
            {
                sqlBuilder.Trail(limit);
            }

            if (offset != null)
            {
                sqlBuilder.Trail(" offset ");
                sqlBuilder.Trail(offset);
            }
        }

        public override string GetDropIndexString(string indexName, string tableName)
        {
            return "drop index if exists " + QuoteForColumnName(indexName);
        }

        public override string QuoteForColumnName(string columnName)
        {
            return QuoteString + columnName + QuoteString;
        }

        public override string QuoteForTableName(string tableName)
        {
            return QuoteString + tableName + QuoteString;
        }

        public override string CascadeConstraintsString => " cascade ";

        public override byte DefaultDecimalPrecision => 19;

        public override byte DefaultDecimalScale => 5;

        public override string GetSqlValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            var type = value.GetType();

            if (type == typeof(TimeSpan))
            {
                return ((TimeSpan)value).Ticks.ToString(CultureInfo.InvariantCulture);
            }

            if (type == typeof(DateTimeOffset))
            {
                return ((DateTimeOffset)value).ToString(CultureInfo.InvariantCulture);
            }

            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Boolean:
                    return (bool)value ? "TRUE" : "FALSE";
                default:
                    return base.GetSqlValue(value);
            }
        }
    }
}
