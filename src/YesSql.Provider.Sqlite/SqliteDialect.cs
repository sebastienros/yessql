using System;
using System.Collections.Generic;
using System.Data;
using YesSql.Sql;

namespace YesSql.Provider.Sqlite
{
    public class SqliteDialect : BaseDialect
    {
        private static readonly Dictionary<DbType, string> _columnTypes = new Dictionary<DbType, string>
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
            { DbType.DateTimeOffset, "TEXT" },
            { DbType.Time, "TIME" }, 
            { DbType.Boolean, "BOOL" },
            { DbType.Guid, "UNIQUEIDENTIFIER" }
        };

        static SqliteDialect()
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
                { typeof(TimeSpan), DbType.Time },

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
                { typeof(TimeSpan?), DbType.Time }
            };
        }

        public SqliteDialect()
        {
            Methods.Add("second", new TemplateFunction("cast(strftime('%S', {0}) as int)"));
            Methods.Add("minute", new TemplateFunction("cast(strftime('%M', {0}) as int)"));
            Methods.Add("hour", new TemplateFunction("cast(strftime('%H', {0}) as int)"));
            Methods.Add("day", new TemplateFunction("cast(strftime('%d', {0}) as int)"));
            Methods.Add("month", new TemplateFunction("cast(strftime('%m', {0}) as int)"));
            Methods.Add("year", new TemplateFunction("cast(strftime('%Y', {0}) as int)"));
            Methods.Add("now", new TemplateFunction("DATETIME('now')"));

            Methods.Add("JSON_VALUE", new TemplateFunction("json_extract({0}, {1})"));
            Methods.Add("JSON_MODIFY", new TemplateFunction("json_set({0}, {1}, {2})"));
        }

        public override string Name => "Sqlite";

        public override string IdentityColumnString => "integer primary key autoincrement";

        public override string IdentitySelectString => "; select last_insert_rowid()";
        public override string IdentityLastId => "last_insert_rowid()";

        public override string RandomOrderByClause => "random()";

        public override byte DefaultDecimalPrecision => 19;

        public override byte DefaultDecimalScale => 5;

        public override string GetTypeName(DbType dbType, int? length, byte? precision, byte? scale)
        {
            if (_columnTypes.TryGetValue(dbType, out var value))
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

        public override string GetDropIndexString(string indexName, string tableName)
        {
            return "drop index if exists " + QuoteForColumnName(indexName);
        }

        public override string QuoteForColumnName(string columnName)
        {
            return "[" + columnName + "]";
        }

        public override string QuoteForTableName(string tableName)
        {
            return "[" + tableName + "]";
        }

        public override bool SupportsIfExistsBeforeTableName => true;
    }
}
