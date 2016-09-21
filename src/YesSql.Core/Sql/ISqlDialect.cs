using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using YesSql.Core.Serialization;

namespace YesSql.Core.Sql
{
    public interface ISqlDialect
    {
        string CreateTableString { get; }
        string PrimaryKeyString { get; }
        string NullColumnString { get; }
        bool SupportsUnique { get; }
        bool HasDataTypeInIdentityColumn { get; }
        bool SupportsIdentityColumns { get; }
        string IdentityColumnString { get; }
        string IdentitySelectString { get; }

        string GetTypeName(DbType dbType, int? length, byte precision, byte scale);
        string GetSqlValue(object value);
        string QuoteForTableName(string v);
        string GetDropTableString(string name);
        string QuoteForColumnName(string columnName);
        string GetDropForeignKeyConstraintString(string name);
        string GetAddForeignKeyConstraintString(string name, string[] srcColumns, string destTable, string[] destColumns, bool primaryKey);
        void Page(SqlBuilder sqlBuilder, int offset, int limit);
    }

    public class SqlDialectFactory
    {
        private static readonly Dictionary<string, ISqlDialect> SqlDialects = new Dictionary<string, ISqlDialect>
        {
            {"sqliteconnection", new SqliteDialect()},
            {"sqlconnection", new SqlServerDialect()}
        };

        public static void RegisterSqlDialect(string connectionName, ISqlDialect sqlTypeAdapter)
        {
            SqlDialects[connectionName] = sqlTypeAdapter;
        }

        public static ISqlDialect For(DbConnection connection)
        {
            string connectionName = connection.GetType().Name.ToLower();

            ISqlDialect dialect = null;

            if (!SqlDialects.TryGetValue(connectionName, out dialect))
            {
                throw new ArgumentException("Unknown connection name: " + connectionName);
            }

            return dialect;
        }
    }

    public abstract class BaseDialect : ISqlDialect
    {

        public virtual string CreateTableString => "create table";

        public virtual bool HasDataTypeInIdentityColumn => false;

        public abstract string IdentitySelectString { get; }

        public virtual string IdentityColumnString => "[int] IDENTITY(1,1) primary key";

        public virtual string NullColumnString => String.Empty;

        public virtual string PrimaryKeyString => "primary key";

        public virtual bool SupportsIdentityColumns => true;

        public virtual bool SupportsUnique => true;

        public virtual bool SupportsForeignKeyConstraintInAlterTable => true;

        public virtual string GetAddForeignKeyConstraintString(string name, string[] srcColumns, string destTable, string[] destColumns, bool primaryKey)
        {
            var res = new StringBuilder(200);

            if (SupportsForeignKeyConstraintInAlterTable)
                res.Append(" add");

            res.Append(" constraint ")
                .Append(name)
                .Append(" foreign key (")
                .Append(String.Join(", ", srcColumns))
                .Append(") references ")
                .Append(destTable);

            if (!primaryKey)
            {
                res.Append(" (")
                    .Append(String.Join(", ", destColumns))
                    .Append(')');
            }

            return res.ToString();
        }

        public virtual string GetDropForeignKeyConstraintString(string name)
        {
            return " drop constraint " + name;
        }

        public virtual bool SupportsIfExistsBeforeTableName => false;
        public virtual string CascadeConstraintsString => String.Empty;
        public virtual bool SupportsIfExistsAfterTableName => false;
        public virtual string GetDropTableString(string name)
        {
            var sb = new StringBuilder("drop table ");
            if (SupportsIfExistsBeforeTableName)
            {
                sb.Append("if exists ");
            }

            sb.Append(name).Append(CascadeConstraintsString);

            if (SupportsIfExistsAfterTableName)
            {
                sb.Append(" if exists");
            }
            return sb.ToString();
        }

        public virtual string QuoteForColumnName(string columnName)
        {
            return Quote(columnName);
        }

        public virtual string QuoteForTableName(string tableName)
        {
            return Quote(tableName);
        }

        public virtual string QuoteString => "\"";
        public virtual string DoubleQuoteString => "\"\"";

        protected virtual string Quote(string value)
        {
            return QuoteString + value.Replace(QuoteString, DoubleQuoteString) + QuoteString;
        }

        public abstract string GetTypeName(DbType dbType, int? length, byte precision, byte scale);

        public virtual string GetSqlValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Object:
                case TypeCode.String:
                case TypeCode.Char:
                    return Quote(value.ToString());
                case TypeCode.Boolean:
                    return (bool)value ? "1" : "0";
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                case TypeCode.DateTime:
                    return String.Concat("'", Convert.ToString(value, CultureInfo.InvariantCulture), "'");
            }

            return "null";
        }

        public abstract void Page(SqlBuilder sqlBuilder, int offset, int limit);
    }

    public class SqliteDialect : BaseDialect
    {
        private static Dictionary<DbType, string> ColumnTypes = new Dictionary<DbType, string>
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
            { DbType.AnsiString, "TEXT" },
            { DbType.String, "TEXT" },
            { DbType.AnsiStringFixedLength, "TEXT" },
            { DbType.StringFixedLength, "TEXT" },
            { DbType.Date, "DATE" },
            { DbType.DateTime, "DATETIME" },
            { DbType.Time, "TIME" },
            { DbType.Boolean, "BOOL" },
            { DbType.Guid, "UNIQUEIDENTIFIER" }
        };

        public override string IdentityColumnString => "integer primary key autoincrement";

        public override string IdentitySelectString => "select last_insert_rowid()";

        public override string GetTypeName(DbType dbType, int? length, byte precision, byte scale)
        {
            string value;
            if (ColumnTypes.TryGetValue(dbType, out value))
            {
                return value;
            }

            throw new Exception("DbType not found for: " + dbType);
        }

        public override void Page(SqlBuilder sqlBuilder, int offset, int limit)
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
    }

    public class SqlServerDialect : BaseDialect
    {
        private static Dictionary<DbType, string> ColumnTypes = new Dictionary<DbType, string>
        {
            {DbType.Guid, "UNIQUEIDENTIFIER"},
            {DbType.Binary, "VARBINARY(8000)"},
            {DbType.Time, "DATETIME"},
            {DbType.Date, "DATETIME"},
            {DbType.DateTime, "DATETIME"},
            {DbType.Boolean, "BIT"},
            {DbType.Byte, "TINYINT"},
            {DbType.Currency, "MONEY"},
            {DbType.Decimal, "DECIMAL(19,5)"},
            {DbType.Double, "FLOAT(53)"},
            {DbType.Int16, "SMALLINT"},
            {DbType.UInt16, "SMALLINT"},
            {DbType.Int32, "INT"},
            {DbType.UInt32, "INT"},
            {DbType.Int64, "BIGINT"},
            {DbType.UInt64, "BIGINT"},
            {DbType.Single, "REAL"},
            {DbType.AnsiStringFixedLength, "CHAR(255)"},
            {DbType.AnsiString, "VARCHAR(255)"},
            {DbType.StringFixedLength, "NCHAR(255)"},
            {DbType.String, "NVARCHAR(255)"},
        };

        public override string IdentitySelectString => "select SCOPE_IDENTITY()";

        public override string GetTypeName(DbType dbType, int? length, byte precision, byte scale)
        {
            if (length.HasValue)
            {
                if (length.Value > 4000)
                {
                    if (dbType == DbType.String)
                    {
                        return "NTEXT";
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
                        return $"NVARCHAR({length})";
                    }

                    if (dbType == DbType.AnsiString)
                    {
                        return $"VARCHAR({length})";
                    }

                    if (dbType == DbType.Binary)
                    {
                        return $"VARBINARY({length})";
                    }
                }
            }

            string value;
            if (ColumnTypes.TryGetValue(dbType, out value))
            {
                return value;
            }

            throw new Exception("DbType not found for: " + dbType);
        }

        public override void Page(SqlBuilder sqlBuilder, int offset, int limit)
        {
            if (offset == 0 && limit != 0)
            {
                // Insert LIMIT clause after the select
                var selector = sqlBuilder.GetSelector();
                selector = " TOP " + limit + " " + selector;
                sqlBuilder.Selector(selector);
            }
            else if (offset != 0 || limit != 0)
            {
                sqlBuilder.Trail = "OFFSET " + offset + " ROWS FETCH FIRST " + limit + " ROWS ONLY";
            }
        }

        private const char OpenQuote = '[';

        private const char CloseQuote = ']';

        public override string QuoteForColumnName(string columnName)
        {
            return OpenQuote + columnName + CloseQuote;
        }

        public override string QuoteForTableName(string tableName)
        {
            return OpenQuote + tableName + CloseQuote;
        }


    }
}
