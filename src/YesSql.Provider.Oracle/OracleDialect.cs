using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Dapper;
using Dapper.Oracle;
using Oracle.ManagedDataAccess.Client;
using YesSql.Indexes;
using YesSql.Sql;
using YesSql.Sql.Schema;

namespace YesSql.Provider.Oracle
{
    public class OracleDialect : BaseDialect
    {
        private const string defaultValueInsertStringForReplace = "defaultValueInsertStringForReplace";
        private static readonly ConcurrentDictionary<string, IEnumerable<TableColumnInfo>> tableColumnInfoCache = new ConcurrentDictionary<string, IEnumerable<TableColumnInfo>>();
        private static readonly ConcurrentDictionary<string, int> defaultCountCache = new ConcurrentDictionary<string, int>();

        private static readonly Dictionary<DbType, string> ColumnTypes = new Dictionary<DbType, string>
        {
            {DbType.Binary, "blob"},//typeof(object), typeof(byte[])
            {DbType.DateTime, "timestamp" },//typeof(DateTime),typeof(DateTimeOffset)
            {DbType.Boolean, "number(1)"},//typeof(bool), 
            {DbType.Decimal, "number"},//typeof(decimal)
            {DbType.Single, "binary_float"},//typeof(float)
            {DbType.Double, "binary_double"},//typeof(double)
            {DbType.Int16, "number(5,0)"},//typeof(short)
            {DbType.Int32, "number(9,0)"},//typeof(int)
            {DbType.Int64, "number(19,0)"},//typeof(long)
            {DbType.UInt16, "number(5,0)"},//,typeof(ushort)
            {DbType.UInt32, "number(9,0)"},// typeof(uint)
            {DbType.UInt64, "number(19,0)"},//typeof(ulong)
            {DbType.String, "nvarchar2(255)"},//typeof(Guid), typeof(string),typeof(char)
            {DbType.SByte, "integer"}//typeof(sbyte)
        };

        private static readonly string[] UnsafeParameters =
        {
            "Order",
            "Date",
            "Version"
        };
        private static readonly string safeParameterSuffix = "Safe";

        public OracleDialect()
        {
            Methods.Add("second", new TemplateFunction("extract(second from {0})"));
            Methods.Add("minute", new TemplateFunction("extract(minute from {0})"));
            Methods.Add("hour", new TemplateFunction("extract(hour from {0})"));
            Methods.Add("day", new TemplateFunction("extract(day from {0})"));
            Methods.Add("month", new TemplateFunction("extract(month from {0})"));
            Methods.Add("year", new TemplateFunction("extract(year from {0})"));
            SqlMapper.AddTypeMap(typeof(bool), DbType.Int32);
            SqlMapper.AddTypeMap(typeof(uint), DbType.Int32);
        }

        public override string Name => "Oracle";
        public override bool IsSpecialDistinctRequired => true;

        public override string GetTypeName(DbType dbType, int? length, byte precision, byte scale)
        {
            if (length.HasValue)
            {
                if (dbType == DbType.String)
                {
                    return length.Value > 2000 ? "nclob" : $"nvarchar2({length})";
                }

                if (dbType == DbType.Binary)
                {
                    return length.Value > 4000 ? "blob" : $"raw({length})";
                }
            }

            if (ColumnTypes.TryGetValue(dbType, out var value))
            {
                return value;
            }

            throw new Exception("DbType not found for: " + dbType);
        }

        public override string DefaultValuesInsert => defaultValueInsertStringForReplace;

        public override void Page(ISqlBuilder sqlBuilder, string offset, string limit)
        {
            //only Oracle 12c
            if (offset != null)
            {
                sqlBuilder.Trail(" OFFSET ");
                sqlBuilder.Trail(offset);
                sqlBuilder.Trail(" ROWS");
            }

            if (limit != null)
            {
                sqlBuilder.Trail(" FETCH NEXT ");
                sqlBuilder.Trail(limit);
                sqlBuilder.Trail(" ROWS ONLY");
            }
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

        public override string CascadeConstraintsString => " cascade constraint ";

        public override bool HasDataTypeInIdentityColumn => true;
        public override bool SupportsIdentityColumns => true;
        public override string IdentitySelectString => "RETURNING";
        public override string IdentityColumnString => " GENERATED ALWAYS AS IDENTITY primary key"; //only available in Oracle 12c
        public override string QuoteForParameter(string parameterName)
        {
            if (UnsafeParameters.Contains(parameterName, StringComparer.InvariantCultureIgnoreCase))
            {
                return ParameterNamePrefix + parameterName + safeParameterSuffix;
            }
            return ParameterNamePrefix + parameterName;
        }
        public override string GetSqlValue(object value)
        {
            if (value == null)
            {
                return NullString;
            }

            return base.GetSqlValue(value);
        }
        public override string NullString => "null";
        public override string ParameterNamePrefix => ":";
        public override string StatementEnd => "";
        public override IDbCommand ConfigureCommand(IDbCommand command)
        {
            var oracleCommand = (OracleCommand)command;
            oracleCommand.BindByName = true;
            return oracleCommand;
        }
        public override int InsertReturningIndexId(DbConnection connection, IIndex index, string insertSql, DbTransaction transaction)
        {
            if (insertSql.Contains(defaultValueInsertStringForReplace))
            {
                insertSql = InsertDefaultValues(connection, insertSql, transaction);
            }
            var sql = insertSql + $" {IdentitySelectString} {QuoteForColumnName("Id")} INTO :id";
            var parameters = new DynamicParameters(index);
            if (IsContainSafeParameters(sql))
            {
                parameters = GetSafeParameters(index);
            }
            parameters.Add(":id", 0, DbType.Int32, ParameterDirection.Output);

            connection.Execute(sql, parameters, transaction);
            var result = parameters.Get<int>(":id");
            return result;
        }

        private static string InsertDefaultValues(DbConnection connection, string insertSql, DbTransaction transaction)
        {
            var firstIndex = "INSERT INTO \"".Count();
            var lenghtTableName = insertSql.IndexOf("\"", firstIndex + 1) - firstIndex;
            var tableName = insertSql.Substring(firstIndex, lenghtTableName);
            if (!defaultCountCache.TryGetValue(tableName, out var count))
            {
                var sqlForDataTypes = $"SELECT COUNT(column_name) as \"ColumnName\"" +
                        $" FROM all_tab_columns where table_name = \'{tableName}\'";
                count = connection.ExecuteScalar<int>(sqlForDataTypes, transaction: transaction);
                defaultCountCache.TryAdd(tableName, count);
            }
            var defaultValues = $"VALUES({String.Join(",", Enumerable.Repeat("DEFAULT", count))})";
            insertSql = insertSql.Replace(defaultValueInsertStringForReplace, defaultValues);
            return insertSql;
        }

        public override string GetDropIndexString(string indexName, string tableName)
        {
            return $"drop index \"{indexName}\"";
        }
        public override object GetDynamicParameters(DbConnection connection, object parameters, string tableName)
        {
            return GetOracleDynamicParameters(connection, parameters, tableName);
        }

        public override object GetSafeIndexParameters(IIndex index)
        {
            return GetSafeParameters(index);
        }

        private OracleDynamicParameters GetOracleDynamicParameters(DbConnection connection, object parameters, string tableName)
        {
            if (!tableColumnInfoCache.TryGetValue(tableName, out var dataTypes))
            {
                var sqlForDataTypes = $"SELECT column_name as \"ColumnName\", data_type as \"DataType\" " +
                             $" FROM all_tab_columns where table_name = \'{tableName}\'";
                dataTypes = connection.Query<TableColumnInfo>(sqlForDataTypes);
                tableColumnInfoCache.TryAdd(tableName, dataTypes);
            }
            var type = parameters.GetType();
            var result = new OracleDynamicParameters();
            foreach (var property in type.GetProperties())
            {
                var value = property.GetValue(parameters);
                var tableColumnInfo = dataTypes.FirstOrDefault(dt => dt.ColumnName == property.Name);
                if (tableColumnInfo != null)
                {
                    var propertyName = property.Name;
                    if (UnsafeParameters.Contains(propertyName, StringComparer.InvariantCultureIgnoreCase))
                    {
                        propertyName += safeParameterSuffix;
                    }
                    result.Add(propertyName, value, tableColumnInfo.OracleMappingType);
                }
            }

            return result;
        }

        private bool IsContainSafeParameters(string sql)
        {
            return UnsafeParameters.Any(parameter => sql.Contains(ParameterNamePrefix + parameter + safeParameterSuffix));
        }

        private DynamicParameters GetSafeParameters(IIndex index)
        {
            var parameters = new DynamicParameters();
            var type = index.GetType();
            foreach (var property in type.GetProperties())
            {
                var value = property.GetValue(index);
                var propertyName = property.Name;
                if (UnsafeParameters.Contains(propertyName, StringComparer.InvariantCultureIgnoreCase))
                {
                    propertyName += safeParameterSuffix;
                }
                var dbType = SchemaUtils.ToDbType(property.PropertyType);
                if (property.PropertyType == typeof(bool))
                {
                    dbType = DbType.Int32;
                }

                parameters.Add(propertyName, value, dbType);
            }
            return parameters;
        }
    }
}
