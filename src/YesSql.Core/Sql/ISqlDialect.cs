using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using YesSql.Core.Sql.Providers.MySql;
using YesSql.Core.Sql.Providers.Sqlite;
using YesSql.Core.Sql.Providers.SqlServer;

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
        ISqlBuilder CreateBuilder(string tablePrefix);
    }

    public class SqlDialectFactory
    {
        public static Dictionary<string, ISqlDialect> SqlDialects { get; } = new Dictionary<string, ISqlDialect>
        {
            {"sqliteconnection", new SqliteDialect()},
            {"sqlconnection", new SqlServerDialect()},
            {"mysqlconnection", new MySqlDialect() }
        };

        public static void RegisterSqlDialect(string connectionName, ISqlDialect sqlTypeAdapter)
        {
            SqlDialects[connectionName] = sqlTypeAdapter;
        }

        public static ISqlDialect For(DbConnection connection)
        {
            string connectionName = connection.GetType().Name.ToLower();

            if (!SqlDialects.TryGetValue(connectionName, out ISqlDialect dialect))
            {
                throw new ArgumentException("Unknown connection name: " + connectionName);
            }

            return dialect;
        }
    }
}
