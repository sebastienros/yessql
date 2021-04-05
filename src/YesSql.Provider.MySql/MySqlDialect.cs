using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using YesSql.Sql;
using YesSql.Utils;

namespace YesSql.Provider.MySql
{
    public class MySqlDialect : BaseDialect
    {
        private static readonly Dictionary<DbType, string> _columnTypes = new Dictionary<DbType, string>
        {
            {DbType.Guid, "char(36)"},
            {DbType.Binary, "varbinary(8000)"},
            {DbType.Time, "time"},
            {DbType.Date, "datetime"},
            {DbType.DateTime, "datetime" },
            {DbType.DateTime2, "datetime" },
            {DbType.DateTimeOffset, "varchar(255)" },
            {DbType.Boolean, "bit"},
            {DbType.Byte, "tinyint unsigned"},
            {DbType.SByte, "tinyint unsigned"},
            {DbType.Decimal, "decimal({0}, {1})"},
            {DbType.Double, "double"},
            {DbType.Single, "float"},
            {DbType.Int16, "smallint"},
            {DbType.UInt16, "smallint unsigned"},
            {DbType.Int32, "int"},
            {DbType.UInt32, "int unsigned"},
            {DbType.Int64, "bigint"},
            {DbType.UInt64, "bigint unsigned"},
            {DbType.AnsiStringFixedLength, "char"},
            {DbType.AnsiString, "varchar(127)"},
            {DbType.StringFixedLength, "char"},
            {DbType.String, "varchar(255)"},
        };

        static MySqlDialect()
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
                { typeof(TimeSpan?), DbType.Int64 }
            };
        }

        public MySqlDialect()
        {
            AddTypeHandler<TimeSpan, long>(x => x.Ticks);
            Methods.Add("now", new TemplateFunction("UTC_TIMESTAMP()"));
        }

        public override string Name => "MySql";
        public override string IdentitySelectString => "; select LAST_INSERT_ID()";
        public override string IdentityLastId => "LAST_INSERT_ID()";
        public override string IdentityColumnString => "int AUTO_INCREMENT primary key";
        public override string RandomOrderByClause => "rand()";
        public override bool SupportsIfExistsBeforeTableName => true;

        public override string GetTypeName(DbType dbType, int? length, byte? precision, byte? scale)
        {
            if (length.HasValue)
            {
                if (dbType == DbType.Binary)
                {
                    if (length < 256)
                    {
                        return "TINYBLOB";
                    }

                    if (length < 65536)
                    {
                        return "BLOB";
                    }

                    if (length < 16777216)
                    {
                        return "MEDIUMBLOB";
                    }

                    return "LONGBLOB";
                }

                if (length.Value > 4000)
                {
                    if (dbType == DbType.String)
                    {
                        // Mysql uses up to 4 bytes per Unicode char depends on Encoding, so 65536/4 and 16MB/4 make sense
                        return length.Value > 16384 ?
                            length.Value > 4194304 ? "LONGTEXT" : "MEDIUMTEXT" : "TEXT";
                    }

                    if (dbType == DbType.AnsiString)
                    {
                        // Mysql uses up to 4 bytes per Unicode char depends on Encoding, so 65536/4 and 16MB/4 make sense
                        return length.Value > 16384 ?
                            length.Value > 4194304 ? "LONGTEXT" : "MEDIUMTEXT" : "TEXT";
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

        public override string FormatKeyName(string name)
        {
            // https://dev.mysql.com/doc/refman/8.0/en/identifier-length.html
            // MySql limits identifiers to 64 char.
            if (name.Length >= 64)
            {
                return HashHelper.HashName("FK_", name);
            }

            return name;
        }

        public override string FormatIndexName(string name)
        {
            // https://dev.mysql.com/doc/refman/8.0/en/identifier-length.html
            // MySql limits identifiers to 64 char.
            if (name.Length >= 64)
            {
                return HashHelper.HashName("IDX_FK_", name);
            }

            return name;
        }        

        public override string GetDropForeignKeyConstraintString(string name)
        {
            return " drop foreign key " + FormatKeyName(name);
        }

        public override string GetAddForeignKeyConstraintString(string name, string[] srcColumns, string destTable, string[] destColumns, bool primaryKey)
        {
            string sql = base.GetAddForeignKeyConstraintString(name, srcColumns, destTable, destColumns, primaryKey);

            var res = new StringBuilder(sql);

            res.Append(" on delete cascade ")
                .Append(" on update cascade ");

            return res.ToString();
        }

        public override string DefaultValuesInsert => "VALUES()";

        public override byte DefaultDecimalPrecision => 65;

        public override byte DefaultDecimalScale => 30;

        public override void Page(ISqlBuilder sqlBuilder, string offset, string limit)
        {
            if (offset != null && limit == null)
            {
                limit = "-1";
            }

            if (limit != null)
            {
                sqlBuilder.Trail(" LIMIT ");

                // c.f. https://stackoverflow.com/questions/255517/mysql-offset-infinite-rows
                sqlBuilder.Trail(limit == "-1" ? "18446744073709551610" : limit);

                if (offset != null)
                {
                    sqlBuilder.Trail(" OFFSET ");
                    sqlBuilder.Trail(offset);
                }
            }
        }

        public override string GetDropIndexString(string indexName, string tableName)
        {
            // This is dependent on version of MySql < v10.1.4 does not support IF EXISTS
            return "drop index " + QuoteForColumnName(indexName) + " on " + QuoteForTableName(tableName);
        }

        public override string QuoteForColumnName(string columnName)
        {
            return "`" + columnName + "`";
        }

        public override string QuoteForTableName(string tableName)
        {
            return "`" + tableName + "`";
        }

        public override void Concat(IStringBuilder builder, params Action<IStringBuilder>[] generators)
        {
            builder.Append("concat(");

            for (var i = 0; i < generators.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
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
    }
}
