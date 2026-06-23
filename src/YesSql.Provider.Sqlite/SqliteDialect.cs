using System;
using System.Collections.Generic;
using System.Data;
using YesSql.Sql;

namespace YesSql.Provider.Sqlite
{
    /// <summary>
    /// Represents the SQL dialect for SQLite.
    /// </summary>
    public sealed class SqliteDialect : BaseDialect
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteDialect"/> class.
        /// </summary>
        public SqliteDialect()
        {
            // Add type handlers for cross-type DateTime/DateTimeOffset compatibility
            // Convert DateTimeOffset to DateTime for consistent storage and comparison
            AddTypeHandler<DateTimeOffset, DateTime>(x => x.UtcDateTime);

            Methods.Add("second", new TemplateFunction("cast(strftime('%S', {0}) as int)"));
            Methods.Add("minute", new TemplateFunction("cast(strftime('%M', {0}) as int)"));
            Methods.Add("hour", new TemplateFunction("cast(strftime('%H', {0}) as int)"));
            Methods.Add("day", new TemplateFunction("cast(strftime('%d', {0}) as int)"));
            Methods.Add("month", new TemplateFunction("cast(strftime('%m', {0}) as int)"));
            Methods.Add("year", new TemplateFunction("cast(strftime('%Y', {0}) as int)"));
            Methods.Add("now", new TemplateFunction("DATETIME('now')"));
        }

        /// <inheritdoc />
        public override string Name => "Sqlite";

        /// <inheritdoc />
        public override string IdentityColumnString => "integer primary key autoincrement";
        /// <inheritdoc />
        public override string LegacyIdentityColumnString => "integer primary key autoincrement";
        /// <inheritdoc />
        public override string IdentitySelectString => "; select last_insert_rowid()";
        /// <inheritdoc />
        public override string IdentityLastId => "last_insert_rowid()";

        /// <inheritdoc />
        public override string RandomOrderByClause => "random()";

        /// <inheritdoc />
        public override byte DefaultDecimalPrecision => 19;

        /// <inheritdoc />
        public override byte DefaultDecimalScale => 5;

        /// <inheritdoc />
        public override string GetTypeName(DbType dbType, int? length, byte? precision, byte? scale)
        {
            if (_columnTypes.TryGetValue(dbType, out var value))
            {
                return value;
            }

            throw new Exception("DbType not found for: " + dbType);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override string GetDropIndexString(string indexName, string tableName, string schema)
        {
            return "drop index if exists " + QuoteForColumnName(indexName);
        }

        /// <inheritdoc />
        public override string QuoteForColumnName(string columnName)
        {
            return "[" + columnName + "]";
        }

        /// <inheritdoc />
        public override string QuoteForTableName(string tableName, string schema)
        {
            return "[" + tableName + "]";
        }

        /// <inheritdoc />
        public override string QuoteForAliasName(string aliasName)
        {
            return aliasName;
        }

        /// <inheritdoc />
        public override bool SupportsIfExistsBeforeTableName => true;

        /// <inheritdoc />
        /// <remarks>
        /// Batching is disabled for SQLite. SQLite runs in-process, so grouping many statements
        /// into a single command provides no round-trip savings. Worse, Microsoft.Data.Sqlite binds
        /// every parameter of the command to every statement it contains, making parameter binding
        /// O(statements × parameters). For large batches this dominates both CPU and allocations.
        /// Executing the commands individually is significantly faster and allocates far less.
        /// </remarks>
        public override bool SupportsBatching => false;

        /// <inheritdoc />
        public override string GetCreateSchemaString(string schema)
        {
            return null;
        }

        /// <inheritdoc />
        public override IEnumerable<(string aggregate, string alias)> GetAggregateOrders(IList<string> select, IList<string> orderBy)
        {
            // Most databases (MySql, PostgreSql and SqlServer) require all ordered fields to be part of the select when GROUP BY (or DISTINCT) is used

            var result = new List<(string, string)>();

            var index = 1;

            for (var i = 0; i < orderBy.Count; i++)
            {
                var o = orderBy[i];
                var next = i + 1 < orderBy.Count ? orderBy[i + 1].Trim() : null;
                var trimmed = o.Trim();
                var alias = QuoteForAliasName("order_" + index++);

                // Each order segment can be a field name, or a punctuation, so we filter out the punctuations 
                if (trimmed != "," && trimmed != "DESC" && trimmed != "ASC")
                {
                    // Don't aggregate the order field in Sqlite
                    var aggregate = $"{o} AS {alias}";

                    if (next == "DESC" || next == "ASC")
                    {
                        alias += " " + next;
                    }

                    result.Add((aggregate, alias));
                }
            }

            return result;
        }
    }
}
