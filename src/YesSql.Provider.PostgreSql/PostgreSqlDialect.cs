using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using YesSql.Provider;

namespace YesSql.Providers.PostgreSql
{
    public class PostgreSqlDialect : BaseDialect
    {
        private static Dictionary<DbType, string> ColumnTypes = new Dictionary<DbType, string>
        {
            {DbType.Guid, "char(36)"},
            {DbType.Binary, "varbinary"},
            {DbType.Date, "date"},
            {DbType.Time, "time"},
            {DbType.DateTime, "timestamp" },
            {DbType.DateTime2, "timestamp" },
            {DbType.DateTimeOffset, "timestamp" },
            {DbType.Boolean, "boolean"},
            {DbType.Byte, "int2"},
            {DbType.Decimal, "decimal(19, 5)"},
            {DbType.Single, "float4"},
            {DbType.Double, "float8"},
            {DbType.Int16, "int2"},
            {DbType.Int32, "int4"},
            {DbType.Int64, "int8"},
            {DbType.UInt16, "int2"},
            {DbType.UInt32, "int4"},
            {DbType.UInt64, "int8"},
            {DbType.AnsiStringFixedLength, "char(255)"},
            {DbType.AnsiString, "varchar(255)"},
            {DbType.StringFixedLength, "char(255)"},
            {DbType.String, "varchar(255)"},
            {DbType.Currency, "decimal(16,4)"}
        };

        public override string Name => "PostgreSql";
        public override string InOperator(string values) => " = any(array[" + values + "])";
        public override string IdentitySelectString => "RETURNING ";
        public override string IdentityColumnString => "SERIAL PRIMARY KEY";
        public override bool SupportsIfExistsBeforeTableName => true;

        public override ISqlBuilder CreateBuilder(string tablePrefix)
        {
            return new PostgreSqlSqlBuilder(tablePrefix, this);
        }

        public override string GetTypeName(DbType dbType, int? length, byte precision, byte scale)
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

                    if (dbType == DbType.Binary)
                    {
                        return "bytea";
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
                        return "bytea";
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

        public override string DefaultValuesInsert => "DEFAULT VALUES";

        public override void Page(ISqlBuilder sqlBuilder, int offset, int limit)
        {
            var sb = new StringBuilder();

            sb.Append(" limit ");

            if (limit != 0)
            {
                sb.Append(limit);
            }

            if (offset != 0)
            {
                sb.Append(" offset ");
                sb.Append(offset);
            }

            sqlBuilder.Trail = sb.ToString();
        }

        public override string QuoteForColumnName(string columnName)
        {
            return QuoteString + columnName + QuoteString;
        }

        public override string QuoteForTableName(string tableName)
        {
            return QuoteString + tableName + QuoteString;
        }

        protected override string Quote(string value)
        {
            return SingleQuoteString + value.Replace(SingleQuoteString, DoubleSingleQuoteString) + SingleQuoteString;
        }

        public override string CascadeConstraintsString => " cascade ";

        public override string GetSqlValue(object value)
        {
            if (value == null)
            {
                return "null";
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
