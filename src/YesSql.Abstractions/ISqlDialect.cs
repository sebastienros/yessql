using System.Data;

namespace YesSql
{
    public interface ISqlDialect
    {
        string Name { get; }
        string CascadeConstraintsString { get; }
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
        string InOperator(string values);
        string InSelectOperator(string query);
        string NotInOperator(string values);
        string NotInSelectOperator(string query);
        string GetDropForeignKeyConstraintString(string name);
        string GetAddForeignKeyConstraintString(string name, string[] srcColumns, string destTable, string[] destColumns, bool primaryKey);
        string DefaultValuesInsert { get; }
        void Page(ISqlBuilder sqlBuilder, int offset, int limit);
        ISqlBuilder CreateBuilder(string tablePrefix);
        string RenderMethod(string name, params string[] args);
    }
}