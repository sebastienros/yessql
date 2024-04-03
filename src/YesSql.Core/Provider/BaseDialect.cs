using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using YesSql.Sql;

namespace YesSql.Provider
{
    /// <summary>
    /// Used to create custom dialects.
    /// </summary>
    public abstract class BaseDialect : ISqlDialect
    {
        public readonly Dictionary<string, ISqlFunction> Methods = new(StringComparer.OrdinalIgnoreCase);

        protected static Dictionary<Type, DbType> _propertyTypes;

        public DbType ToDbType(Type type)
        {
            DbType dbType;

            if (_propertyTypes.TryGetValue(type, out dbType))
            {
                return dbType;
            }

            if (type.IsEnum)
            {
                return DbType.Int32;
            }

            // Nullable<T> ?
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var nullable = Nullable.GetUnderlyingType(type);

                if (nullable != null)
                {
                    return ToDbType(nullable);
                }
            }

            return DbType.Object;
        }

        public virtual object TryConvert(object source)
        {
            if (source == null)
            {
                return source;
            }

            if (_typeHandlers.Count > 0)
            {
                if (_typeHandlers.TryGetValue(source.GetType(), out var handlers) && handlers.Count > 0)
                {
                    foreach (var handler in handlers)
                    {
                        source = handler(source);
                    }

                    return source;
                }
            }

            if (source.GetType().IsEnum)
            {
                return Convert.ToInt32(source);
            }

            return source;
        }

        public abstract string Name { get; }
        public virtual string InOperator(string values)
        {
            if (values.StartsWith("@") && !values.Contains(','))
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
        public abstract string IdentityLastId { get; }
        
        public abstract string IdentityColumnString { get; }
        public abstract string LegacyIdentityColumnString { get; }

        public virtual string NullColumnString => string.Empty;

        public virtual string PrimaryKeyString => "primary key";

        public abstract string RandomOrderByClause { get; }

        public virtual bool SupportsBatching => true;
        public virtual bool SupportsIdentityColumns => true;

        public virtual bool SupportsUnique => true;

        public virtual bool SupportsForeignKeyConstraintInAlterTable => true;

        public virtual string FormatKeyName(string name) => name;
        public virtual string FormatIndexName(string name) => name;

        public virtual string GetAddForeignKeyConstraintString(string name, string[] srcColumns, string destQuotedTable, string[] destColumns, bool primaryKey)
        {
            var res = new StringBuilder(200);

            if (SupportsForeignKeyConstraintInAlterTable)
            {
                res.Append(" add");
            }

            res.Append(" constraint ")
                .Append(name)
                .Append(" foreign key (")
                .AppendJoin(", ", srcColumns)
                .Append(") references ")
                .Append(destQuotedTable);

            if (!primaryKey)
            {
                res.Append(" (")
                    .Append(string.Join(", ", destColumns))
                    .Append(')');
            }

            return res.ToString();
        }

        public virtual string GetDropForeignKeyConstraintString(string name)
        {
            return " drop constraint " + name;
        }

        public virtual bool SupportsIfExistsBeforeTableName => false;
        public virtual string CascadeConstraintsString => string.Empty;
        public virtual bool SupportsIfExistsAfterTableName => false;
        public virtual string GetDropTableString(string tableName, string schema)
        {
            var sb = new StringBuilder("drop table ");
            if (SupportsIfExistsBeforeTableName)
            {
                sb.Append("if exists ");
            }

            sb.Append(QuoteForTableName(tableName, schema)).Append(CascadeConstraintsString);

            if (SupportsIfExistsAfterTableName)
            {
                sb.Append(" if exists");
            }

            return sb.ToString();
        }
        public abstract string GetDropIndexString(string indexName, string tableName, string schema);
        public abstract string QuoteForColumnName(string columnName);
        public abstract string QuoteForTableName(string tableName, string schema);
        public abstract string QuoteForAliasName(string aliasName);

        public virtual string QuoteString => "\"";
        public virtual string DoubleQuoteString => "\"\"";
        public virtual string SingleQuoteString => "'";
        public virtual string DoubleSingleQuoteString => "''";

        public virtual string DefaultValuesInsert => "DEFAULT VALUES";

        public virtual bool PrefixIndex => false;

        public abstract byte DefaultDecimalPrecision { get; }

        public abstract byte DefaultDecimalScale { get; }

        public virtual int MaxCommandsPageSize => int.MaxValue;

        public virtual int MaxParametersPerCommand => int.MaxValue;

        protected virtual string Quote(string value)
        {
            return SingleQuoteString + value.Replace(SingleQuoteString, DoubleSingleQuoteString) + SingleQuoteString;
        }

        public abstract string GetTypeName(DbType dbType, int? length, byte? precision, byte? scale);

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
                case TypeCode.Byte:
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
                    return string.Concat("'", Convert.ToString(value, CultureInfo.InvariantCulture), "'");
                default: break;
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

            return name + "(" + string.Join(", ", args) + ")";
        }

        public virtual void Concat(IStringBuilder builder, params Action<IStringBuilder>[] generators)
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
            return select;
        }

        private readonly Dictionary<Type, List<Func<object, object>>> _typeHandlers = new();

        public void ResetTypeHandlers()
        {
            _typeHandlers.Clear();
        }

        public void AddTypeHandler<T, U>(Func<T, U> handler)
        {
            if (!_typeHandlers.TryGetValue(typeof(T), out var handlers))
            {
                _typeHandlers[typeof(T)] = handlers = new List<Func<object, object>>();
            }

            handlers.Add(i => handler((T)i));
        }

        public virtual string GetCreateSchemaString(string schema)
        {
            return $"CREATE SCHEMA {QuoteForColumnName(schema)}";
        }

        public virtual IEnumerable<(string aggregate, string alias)> GetAggregateOrders(IList<string> select, IList<string> orderBy)
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
                    var aggregate = $"MAX({o}) AS {alias}";

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
