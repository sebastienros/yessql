using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using YesSql.Sql;
using YesSql.Utils;

namespace YesSql.Provider.PostgreSql
{
    /// <summary>
    /// Represents the SQL dialect for PostgreSQL.
    /// </summary>
    public sealed class PostgreSqlDialect : BaseDialect
    {
        private static readonly Dictionary<DbType, string> _columnTypes = new Dictionary<DbType, string>
        {
            {DbType.Guid, "uuid"},
            {DbType.Binary, "bytea"},
            {DbType.Date, "date"},
            {DbType.Time, "time"},
            {DbType.DateTime, "timestamp" },
            {DbType.DateTime2, "timestamp" },
            {DbType.DateTimeOffset, "timestamptz" },
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
                { typeof(DateTimeOffset), DbType.DateTimeOffset },
                { typeof(Guid), DbType.Guid },
                { typeof(TimeSpan), DbType.Int64 }, // stored as ticks

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
                { typeof(TimeSpan?), DbType.Int64 }
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlDialect"/> class.
        /// </summary>
        public PostgreSqlDialect()
        {
            AddTypeHandler<TimeSpan, long>(x => x.Ticks);

            // DateTimes needs to be stored as Utc in timesstamp fields since npgsql 6.0.
            // Can represents a Date & Time without TZ. Utc is forced by keeps the original date and time values.
            AddTypeHandler<DateTime, DateTime>(x => x.Kind != DateTimeKind.Utc ? new DateTime(x.Ticks, DateTimeKind.Utc) : x);

            // DateTimeOffset are stored as Utc DateTimes in timesstamptz fields
            // Represents a moment in time
            AddTypeHandler<DateTimeOffset, DateTime>(x => x.UtcDateTime);

            Methods.Add("second", new TemplateFunction("extract(second from {0})"));
            Methods.Add("minute", new TemplateFunction("extract(minute from {0})"));
            Methods.Add("hour", new TemplateFunction("extract(hour from {0})"));
            Methods.Add("day", new TemplateFunction("extract(day from {0})"));
            Methods.Add("month", new TemplateFunction("extract(month from {0})"));
            Methods.Add("year", new TemplateFunction("extract(year from {0})"));
            Methods.Add("now", new TemplateFunction("now() at time zone 'utc'"));
        }

        /// <inheritdoc />
        public override string Name => "PostgreSql";
        /// <inheritdoc />
        public override string InOperator(string values) => " = any(array[" + values + "])";
        /// <inheritdoc />
        public override string NotInOperator(string values) => " <> all(array[" + values + "])";
        /// <inheritdoc />
        public override string IdentitySelectString => "RETURNING";
        /// <inheritdoc />
        public override string IdentityLastId => $"lastval()";
        /// <inheritdoc />
        public override string IdentityColumnString => "BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY";
        /// <inheritdoc />
        public override string LegacyIdentityColumnString => "INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY";
        /// <inheritdoc />
        public override string RandomOrderByClause => "random()";
        /// <inheritdoc />
        public override bool SupportsIfExistsBeforeTableName => true;
        /// <inheritdoc />
        public override bool PrefixIndex => true;
        /// <inheritdoc />
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

            if (_columnTypes.TryGetValue(dbType, out var value))
            {
                if (dbType == DbType.Decimal)
                {
                    value = string.Format(value, precision ?? DefaultDecimalPrecision, scale ?? DefaultDecimalScale);
                }

                return value;
            }

            throw new Exception("DbType not found for: " + dbType);
        }

        /// <inheritdoc />
        public override string FormatKeyName(string name)
        {
            // https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS
            // Postgres limits identifiers to NAMEDATALEN-1 char, where NAMEDATALEN is 64.
            if (name.Length >= 63)
            {
                return HashHelper.HashName("FK_", name);
            }

            return name;
        }
        
        /// <inheritdoc />
        public override string FormatIndexName(string name)
        {
            // https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS
            // Postgres limits identifiers to NAMEDATALEN-1 char, where NAMEDATALEN is 64.
            if (name.Length >= 63)
            {
                return HashHelper.HashName("IDX_FK_", name);
            }

            return name;
        }  
        /// <inheritdoc />
        public override string GetDropForeignKeyConstraintString(string name)
        {
            return " drop constraint " + QuoteForColumnName(name);
        }

        /// <inheritdoc />
        public override string DefaultValuesInsert => "DEFAULT VALUES";

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override string GetDropIndexString(string indexName, string tableName, string schema)
        {
            return "drop index if exists " + QuoteForColumnName(indexName);
        }

        /// <inheritdoc />
        public override string QuoteForColumnName(string columnName)
        {
            return QuoteString + columnName + QuoteString;
        }

        /// <inheritdoc />
        public override string QuoteForTableName(string tableName, string schema)
        {
            return string.IsNullOrEmpty(schema)
                ? $"{QuoteString}{tableName}{QuoteString}"
                : $"{QuoteString}{schema}{QuoteString}.{QuoteString}{tableName}{QuoteString}"
                ;
        }

        /// <inheritdoc />
        public override string QuoteForAliasName(string aliasName)
        {
            return QuoteString + aliasName + QuoteString;
        }

        /// <inheritdoc />
        public override string CascadeConstraintsString => " cascade ";

        /// <inheritdoc />
        public override byte DefaultDecimalPrecision => 19;

        /// <inheritdoc />
        public override byte DefaultDecimalScale => 5;

        /// <inheritdoc />
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
                return base.GetSqlValue(((DateTimeOffset)value).UtcDateTime);
            }

            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Boolean:
                    return (bool)value ? "TRUE" : "FALSE";
                default:
                    return base.GetSqlValue(value);
            }
        }

        /// <inheritdoc />
        public override string GetCreateSchemaString(string schema)
        {
            return $"CREATE SCHEMA IF NOT EXISTS {QuoteForColumnName(schema)}";
        }
    }
}
