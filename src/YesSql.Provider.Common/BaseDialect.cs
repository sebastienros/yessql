using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using YesSql.Sql;

namespace YesSql.Provider
{
    public abstract class BaseDialect : ISqlDialect
    {
        public Dictionary<string, ISqlFunction> Methods = new Dictionary<string, ISqlFunction>(StringComparer.OrdinalIgnoreCase);

        public abstract string Name { get; }
        public virtual string InOperator(string values)
        {
            if (values.StartsWith("@") && !values.Contains(","))
            {
                return " IN " + values;
            }
            else
            {
                return " IN (" + values + ") ";
            }
        }

        public virtual string NotInOperator(string values)
        {
            return " NOT" + InOperator(values);
        }

        public virtual string InSelectOperator(string values)
        {
            return " IN (" + values + ") ";
        }

        public virtual string NotInSelectOperator(string values)
        {
            return " NOT IN (" + values + ") ";
        }

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
            {
                res.Append(" add");
            }

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

            sb.Append(QuoteForTableName(name)).Append(CascadeConstraintsString);

            if (SupportsIfExistsAfterTableName)
            {
                sb.Append(" if exists");
            }

            return sb.ToString();
        }
        public abstract string GetDropIndexString(string indexName, string tableName);
        public abstract string QuoteForColumnName(string columnName);
        public abstract string QuoteForTableName(string tableName);

        public virtual string QuoteString => "\"";
        public virtual string DoubleQuoteString => "\"\"";
        public virtual string SingleQuoteString => "'";
        public virtual string DoubleSingleQuoteString => "''";

        public virtual string DefaultValuesInsert => "DEFAULT VALUES";

        public virtual bool PrefixIndex => false;

        protected virtual string Quote(string value)
        {
            return SingleQuoteString + value.Replace(SingleQuoteString, DoubleSingleQuoteString) + SingleQuoteString;
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

        public abstract void Page(ISqlBuilder sqlBuilder, string offset, string limit);
        public virtual ISqlBuilder CreateBuilder(string tablePrefix)
        {
            return new SqlBuilder(tablePrefix, this);
        }

        public string RenderMethod(string name, string[] args)
        {
            if (Methods.TryGetValue(name, out var method))
            {
                return method.Render(args);
            }

            return name + "(" + String.Join(", ", args) + ")";
        }

        public virtual void Concat(StringBuilder builder, params Action<StringBuilder>[] generators)
        {
            builder.Append("(");

            for (var i = 0; i < generators.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(" || ");
                }

                generators[i](builder);
            }

            builder.Append(")");
        }

        public virtual List<string> GetDistinctOrderBySelectString(List<string> select, List<string> orderBy)
        {
            // Most databases requires all ordered fields to be part of the select when DISTINCT is used

            foreach (var o in orderBy)
            {
                var trimmed = o.Trim();

                // Each order segment can be a field name, or a punctuation, so we filter out the punctuations 
                if (trimmed != "," && trimmed != "DESC" && trimmed != "ASC" && !select.Contains(o))
                {
                    select.Add(",");
                    select.Add(o);
                }
            }

            return select;
        }
    }
}
