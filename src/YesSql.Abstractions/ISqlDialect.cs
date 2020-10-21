using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace YesSql
{
    public interface ISqlDialect
    {
        /// <summary>
        /// Gets the name of the dialect.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the cascade constraint sql statement.
        /// </summary>
        string CascadeConstraintsString { get; }
        
        /// <summary>
        /// Gets the create table sql statement.
        /// </summary>
        /// <value></value>
        string CreateTableString { get; }

        /// <summary>
        /// Gets the primary key SQL statement.
        /// </summary>
        string PrimaryKeyString { get; }

        /// <summary>
        /// Gets the random OrderBy key SQL statement.
        /// </summary>
        string RandomOrderByClause { get; }

        /// <summary>
        /// Gets the null column SQL statement.
        /// </summary>
        string NullColumnString { get; }

        /// <summary>
        /// Whether the dialect support unique queries.
        /// </summary>
        bool SupportsUnique { get; }

        /// <summary>
        /// Returns whether the index names must be prefixed or not.
        /// </summary>
        bool PrefixIndex { get; }

        /// <summary>
        /// Gets whether the identity columns requires the data type.
        /// </summary>
        bool HasDataTypeInIdentityColumn { get; }

        /// <summary>
        /// Gets whether the dialect supports identity columns.
        /// </summary>
        bool SupportsIdentityColumns { get; }

        /// <summary>
        /// Gets the identity column SQL statement.
        /// </summary>
        string IdentityColumnString { get; }

        /// <summary>
        /// Gets the identity select SQL statement.
        /// </summary>
        string IdentitySelectString { get; }

        /// <summary>
        /// Gets the type name SQL statement.
        /// </summary>
        string GetTypeName(DbType dbType, int? length, byte precision, byte scale);

        /// <summary>
        /// Gets sql value.
        /// </summary>
        string GetSqlValue(object value);

        /// <summary>
        /// Returns the quoted table name.
        /// </summary>
        string QuoteForTableName(string v);

        /// <summary>
        /// Gets the DROP TABLE SQL statement.
        /// </summary>
        string GetDropTableString(string name);

        /// <summary>
        /// Gets the DROP INDEX SQL statement.
        /// </summary>
        string GetDropIndexString(string indexName, string tableName);

        /// <summary>
        /// Returns the quoted column.
        /// </summary>
        string QuoteForColumnName(string columnName);

        /// <summary>
        /// Gets the IN SQL statement.
        /// </summary>
        string InOperator(string values);

        /// <summary>
        /// Gets the IN SELECT SQL statement.
        /// </summary>
        string InSelectOperator(string query);

        /// <summary>
        /// Gets the NOT IN SQL statement.
        /// </summary>
        string NotInOperator(string values);

        /// <summary>
        /// Gets the NOT IN SELECT SQL statement.
        /// </summary>
        string NotInSelectOperator(string query);

        /// <summary>
        /// Returns the DROP FOREIGN KEY constraint SQL statement.
        /// </summary>
        string GetDropForeignKeyConstraintString(string name);

        /// <summary>
        /// Returns the ADD FOREIGN KEY constraint SQL statement.
        /// </summary>
        string GetAddForeignKeyConstraintString(string name, string[] srcColumns, string destTable, string[] destColumns, bool primaryKey);
        
        /// <summary>
        /// Returns the DISTING SELECT SQL statement.
        /// </summary>
        List<string> GetDistinctOrderBySelectString(List<string> select, List<string> orderBy);

        /// <summary>
        /// Concatenates multiple <see cref="StringBuilder" />.
        /// </summary>
        void Concat(StringBuilder builder, params Action<StringBuilder>[] generators);

        /// <summary>
        /// Return the DEFAULT VALUES SQL statement.
        /// </summary>
        string DefaultValuesInsert { get; }

        /// <summary>
        /// Adds the pagination SQL statements.
        /// </summary>
        void Page(ISqlBuilder sqlBuilder, string offset, string limit);

        /// <summary>
        /// Returns a custom method invocation SQL statement.
        /// </summary>
        string RenderMethod(string name, params string[] args);

        /// <summary>
        /// Create a <see cref="ISqlBuilder" /> instance.
        /// </summary>
        ISqlBuilder CreateBuilder(string tablePrefix);
    }
}
