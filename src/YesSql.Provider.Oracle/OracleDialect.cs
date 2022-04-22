using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Dapper.Oracle;
using Oracle.ManagedDataAccess.Client;
using YesSql.Indexes;
using YesSql.Sql;
using YesSql.Utils;

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
            {DbType.Date, "date"},
            {DbType.DateTime, "timestamp" },//typeof(DateTime),typeof(DateTimeOffset)
            {DbType.DateTime2, "timestamp" },//typeof(DateTime),typeof(DateTimeOffset)
            {DbType.Boolean, "number(1)"},//typeof(bool), 
            {DbType.Decimal, "number({0},{1})"},//typeof(decimal)
            {DbType.Single, "binary_float"},//typeof(float)
            {DbType.Double, "binary_double"},//typeof(double)
            {DbType.Int16, "number(5,0)"},//typeof(short)
            {DbType.Int32, "number(9,0)"},//typeof(int)
            {DbType.Int64, "number(19,0)"},//typeof(long)
            {DbType.UInt16, "number(5,0)"},//,typeof(ushort)
            {DbType.UInt32, "number(9,0)"},// typeof(uint)
            {DbType.UInt64, "number(19,0)"},//typeof(ulong)
            {DbType.AnsiString, "varchar2(255)"},
            {DbType.String, "nvarchar2(255)"},//typeof(Guid), typeof(string),typeof(char)
            {DbType.SByte, "integer"}//typeof(sbyte)
        };

        private static readonly string[] UnsafeParameters =
        {
            "Order",
            "Date",
            "Version",
            "Comment",
            "Start",
            "End",
            "Number"
        };
        private static readonly string safeParameterSuffix = "Safe";

        static OracleDialect()
        {
            _propertyTypes = new Dictionary<Type, DbType>()
            {
                { typeof(object), DbType.Binary },
                { typeof(byte[]), DbType.Binary },
                { typeof(string), DbType.String },
                { typeof(char), DbType.String },
                { typeof(bool), DbType.Int32 },
                { typeof(byte), DbType.Byte },
                { typeof(sbyte), DbType.SByte },
                { typeof(short), DbType.Int16 },
                { typeof(ushort), DbType.UInt16 },
                { typeof(int), DbType.Int32 },
                { typeof(uint), DbType.Int32 },
                { typeof(long), DbType.Int64 },
                { typeof(ulong), DbType.Int64 },
                { typeof(float), DbType.Single },
                { typeof(double), DbType.Double },
                { typeof(decimal), DbType.Decimal },
                { typeof(DateTime), DbType.DateTime },
                { typeof(DateTimeOffset), DbType.DateTime }, // stored as UTC datetime    DbType.DateTimeOffset???
                { typeof(Guid), DbType.AnsiString },
                { typeof(TimeSpan), DbType.Int64 }, // stored as ticks

                // Nullable types to prevent extra reflection on common ones
                { typeof(char?), DbType.String },
                { typeof(bool?), DbType.Int32 },
                { typeof(byte?), DbType.Byte },
                { typeof(sbyte?), DbType.Int16 },
                { typeof(short?), DbType.Int16 },
                { typeof(ushort?), DbType.Int16 },
                { typeof(int?), DbType.Int32 },
                { typeof(uint?), DbType.Int32 },
                { typeof(long?), DbType.Int64 },
                { typeof(ulong?), DbType.Int64 },
                { typeof(float?), DbType.Single },
                { typeof(double?), DbType.Double },
                { typeof(decimal?), DbType.Decimal },
                { typeof(DateTime?), DbType.DateTime },
                { typeof(DateTimeOffset?), DbType.DateTime },
                { typeof(Guid?), DbType.String },
                { typeof(TimeSpan?), DbType.Int64 }
            };
        }

        public OracleDialect()
        {
            Methods.Add("second", new TemplateFunction("extract(second from {0})"));
            Methods.Add("minute", new TemplateFunction("extract(minute from {0})"));
            Methods.Add("hour", new TemplateFunction("extract(hour from {0})"));
            Methods.Add("day", new TemplateFunction("extract(day from {0})"));
            Methods.Add("month", new TemplateFunction("extract(month from {0})"));
            Methods.Add("year", new TemplateFunction("extract(year from {0})"));
            Methods.Add("now", new TemplateFunction("SYSDATE"));

            SqlMapper.AddTypeMap(typeof(bool), DbType.Int32);
            SqlMapper.AddTypeMap(typeof(uint), DbType.Int32);
            SqlMapper.AddTypeMap(typeof(Guid), DbType.String);
            AddTypeHandler<Guid, string>(x => x.ToString());
            AddTypeHandler<TimeSpan, long>(x => x.Ticks);
            AddTypeHandler<DateTimeOffset, DateTime>(x => x.UtcDateTime);
        }

        public override string Name => "Oracle";
        public override bool IsSpecialDistinctRequired => true;
        public override bool SupportsBatching => false;
        public override string IdentityLastId => $"lastval()";//used only batch
        public override string RandomOrderByClause => "dbms_random.value";
        public override bool SupportsIfExistsBeforeTableName => false;
        public override bool PrefixIndex => true;
        public override string CascadeConstraintsString => " cascade constraints";
        public override bool HasDataTypeInIdentityColumn => true;
        public override string IdentitySelectString => "RETURNING";
        public override string IdentityColumnString => " GENERATED ALWAYS AS IDENTITY primary key"; //only available in Oracle 12c
        public override byte DefaultDecimalPrecision => 19;
        public override byte DefaultDecimalScale => 5;

        public override string FormatKeyName(string name)
        {
            if (name.Length >= 92)
            {
                return HashHelper.HashName("FK_", name);
            }

            return name;
        }
        
        public override string FormatIndexName(string name)
        {
            if (name.Length >= 92)
            {
                return HashHelper.HashName("IDX_FK_", name);
            }

            return name;
        }

        public override string GetTypeName(DbType dbType, int? length, byte? precision, byte? scale)
        {
            if (length.HasValue)
            {
                if (dbType == DbType.String)
                {
                    return length.Value > 2000 ? "nclob" : $"nvarchar2({length})";
                }

                if (dbType == DbType.AnsiString)
                {
                    return length.Value > 4000 ? "clob" : $"varchar2({length})";
                }

                if (dbType == DbType.Binary)
                {
                    return length.Value > 4000 ? "blob" : $"raw({length})";
                }
            }

            if (ColumnTypes.TryGetValue(dbType, out var value))
            {
                if (dbType == DbType.Decimal)
                {
                    value = String.Format(value, precision ?? DefaultDecimalPrecision, scale ?? DefaultDecimalScale);
                }
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

        public override string QuoteForParameter(string parameterName)
        {
            return ParameterNamePrefix + GetParameterName(parameterName);
        }

        public override string GetParameterName(string parameterName)
        {
            if (UnsafeParameters.Contains(parameterName, StringComparer.InvariantCultureIgnoreCase))
            {
                return parameterName + safeParameterSuffix;
            }
            return parameterName;
        }

        public override string GetSqlValue(object value)
        {
            if (value == null)
            {
                return NullString;
            }
            var type = value.GetType();

            if (type == typeof(TimeSpan))
            {
                return ((TimeSpan)value).Ticks.ToString(CultureInfo.InvariantCulture);
            }

            if (type == typeof(DateTimeOffset))
            {
                return base.GetSqlValue(((DateTimeOffset)value).UtcDateTime);
            }

            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Boolean:
                    return (bool)value ? "1" : "0";
                default:
                    return base.GetSqlValue(value);
            }
        }
        public override string NullString => "null";
        public override string ParameterNamePrefix => ":";
        public override string StatementEnd => String.Empty;
        public override string BatchStatementEnd => ";";
        public override IDbCommand ConfigureCommand(IDbCommand command)
        {
            var oracleCommand = (OracleCommand)command;
            oracleCommand.BindByName = true;
            return oracleCommand;
        }
        public override async Task<int> InsertReturningReduceIndexAsync(DbConnection connection, IIndex index, string insertSql, DbTransaction transaction)
        {
            if (insertSql.Contains(defaultValueInsertStringForReplace))
            {
                insertSql = await InsertDefaultValuesAsync(connection, insertSql, transaction);
            }
            var sql = insertSql + " INTO :id";
            var parameters = new DynamicParameters(index);
            if (IsContainSafeParameters(sql))
            {
                parameters = GetSafeParameters(index);
            }
            parameters.Add(":id", 0, DbType.Int32, ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters, transaction);
            var result = parameters.Get<int>(":id");
            return result;
        }
        public override void PrepareReturningMapIndexCommand(DbCommand command)
        {
            if (command.CommandText.Contains(defaultValueInsertStringForReplace))
            {
                AddDefaultValues(command);
            }
            command.CommandText += " INTO :id";
            if (IsContainSafeParameters(command.CommandText))
            {
                foreach (DbParameter parameter in command.Parameters)
                {
                    if (UnsafeParameters.Contains(parameter.ParameterName, StringComparer.InvariantCultureIgnoreCase))
                    {
                        parameter.ParameterName += safeParameterSuffix;
                    }
                }
            }

            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "id";
            idParameter.Direction = ParameterDirection.Output;
            idParameter.DbType = DbType.Int32;
            idParameter.Value = 0;
            command.Parameters.Add(idParameter);
        }
        private static void AddDefaultValues(DbCommand command)
        {
            var firstIndex = "INSERT INTO \"".Count();
            var lenghtTableName = command.CommandText.IndexOf("\"", firstIndex + 1) - firstIndex;
            var tableName = command.CommandText.Substring(firstIndex, lenghtTableName);
            if (!defaultCountCache.TryGetValue(tableName, out var count))
            {
                var sqlForDataTypes = $"SELECT COUNT(column_name) as \"ColumnName\"" +
                                      $" FROM all_tab_columns where table_name = \'{tableName}\'";
                count = command.Connection.ExecuteScalar<int>(sqlForDataTypes);
                defaultCountCache.TryAdd(tableName, count);
            }
            var defaultValues = $"VALUES({String.Join(",", Enumerable.Repeat("DEFAULT", count))})";
            command.CommandText = command.CommandText.Replace(defaultValueInsertStringForReplace, defaultValues);
        }


        private static async Task<string> InsertDefaultValuesAsync(DbConnection connection, string insertSql, DbTransaction transaction)
        {
            var firstIndex = "INSERT INTO \"".Count();
            var lenghtTableName = insertSql.IndexOf("\"", firstIndex + 1) - firstIndex;
            var tableName = insertSql.Substring(firstIndex, lenghtTableName);
            if (!defaultCountCache.TryGetValue(tableName, out var count))
            {
                var sqlForDataTypes = $"SELECT COUNT(column_name) as \"ColumnName\"" +
                        $" FROM all_tab_columns where table_name = \'{tableName}\'";
                count = await connection.ExecuteScalarAsync<int>(sqlForDataTypes, transaction: transaction);
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
                var dbType = ToDbType(property.PropertyType);
                if (property.PropertyType == typeof(bool))
                {
                    dbType = DbType.Int32;
                }

                parameters.Add(propertyName, value, dbType);
            }
            return parameters;
        }

        public override string AliasKeyword => string.Empty;

        public override string GetAddForeignKeyConstraintString(string name, string[] srcColumns, string destTable, string[] destColumns, bool primaryKey)
        {
            var res = new StringBuilder(200);

            if (SupportsForeignKeyConstraintInAlterTable)
            {
                res.Append(" add");
            }

            res.Append(" constraint \"")
                .Append(name)
                .Append("\" foreign key (")
#if NETSTANDARD2_1
                .AppendJoin(", ", srcColumns)
#else
                .Append(string.Join(", ", srcColumns))
#endif
                .Append(") references ")
                .Append(destTable);

            if (!primaryKey)
            {
                res.Append(" (")
                    .Append(string.Join(", ", destColumns))
                    .Append(')');
            }

            return res.ToString();
        }

    }
}
