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
            {DbType.Binary, "varbinary"},
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
                if (length.Value > 4000)
                {
                    if (dbType == DbType.String)
                    {
                        return "TEXT";
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
                        return "varchar(" + length + ")";
                    }

                    if (dbType == DbType.AnsiString)
                    {
                        return "varchar(" + length + ")";
                    }

                    if (dbType == DbType.Binary)
                    {
                        return "varbinary(" + length + ")";
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
