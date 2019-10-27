using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace YesSql.Provider.MySql
{
    public class MySqlDialect : BaseDialect
    {
        private static Dictionary<DbType, string> ColumnTypes = new Dictionary<DbType, string>
        {
            {DbType.Guid, "char(36)"},
            {DbType.Binary, "varbinary(8000)"},
            {DbType.Time, "time"},
            {DbType.Date, "datetime"},
            {DbType.DateTime, "datetime" },
            {DbType.DateTime2, "datetime" },
            {DbType.DateTimeOffset, "datetime" },
            {DbType.Boolean, "bit"},
            {DbType.Byte, "tinyint unsigned"},
            {DbType.Decimal, "decimal(65, 30)"},
            {DbType.Double, "double"},
            {DbType.Int16, "smallint"},
            {DbType.UInt16, "smallint unsigned"},
            {DbType.Int32, "int"},
            {DbType.UInt32, "int unsigned"},
            {DbType.Int64, "bigint"},
            {DbType.UInt64, "bigint unsigned"},
            {DbType.AnsiStringFixedLength, "char"},
            {DbType.AnsiString, "varchar(127)"},
            {DbType.StringFixedLength, "varchar"},
            {DbType.String, "varchar(255)"},
        };

        public override string Name => "MySql";
        public override string IdentitySelectString => "; select LAST_INSERT_ID()";
        public override string IdentityColumnString => "int AUTO_INCREMENT primary key";
        public override bool SupportsIfExistsBeforeTableName => true;

        public override string GetTypeName(DbType dbType, int? length, byte precision, byte scale)
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

            if (ColumnTypes.TryGetValue(dbType, out string value))
            {
                return value;
            }

            throw new Exception("DbType not found for: " + dbType);
        }

        public override string GetDropForeignKeyConstraintString(string name)
        {
            return " drop foreign key " + name;
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

        public override void Concat(StringBuilder builder, params Action<StringBuilder>[] generators)
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
    }
}
