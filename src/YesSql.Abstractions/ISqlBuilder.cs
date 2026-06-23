using System.Collections.Generic;

namespace YesSql
{
    /// <summary>
    /// A class implementing this interface is able to create custom SQL queries.
    /// </summary>
    public interface ISqlBuilder
    {
        /// <summary>
        /// Gets the type of clause being built (for example a select or an update).
        /// </summary>
        string Clause { get; }

        /// <summary>
        /// Gets the dictionary of named parameters added to the query.
        /// </summary>
        Dictionary<string, object> Parameters { get; }

        /// <summary>
        /// Formats a column reference using the dialect's quoting rules.
        /// </summary>
        /// <param name="table">The name of the table the column belongs to.</param>
        /// <param name="column">The name of the column.</param>
        /// <param name="schema">The schema the table belongs to.</param>
        /// <param name="isAlias">If <c>true</c>, the table is treated as an alias rather than a real table name.</param>
        /// <returns>The formatted column reference.</returns>
        string FormatColumn(string table, string column, string schema, bool isAlias = false);

        /// <summary>
        /// Formats a table reference using the dialect's quoting rules.
        /// </summary>
        /// <param name="table">The name of the table.</param>
        /// <param name="schema">The schema the table belongs to.</param>
        /// <returns>The formatted table reference.</returns>
        string FormatTable(string table, string schema);

        /// <summary>
        /// Gets the current selector clause of the query.
        /// </summary>
        /// <returns>The selector clause.</returns>
        string GetSelector();

        /// <summary>
        /// Gets a value indicating whether the query contains a join.
        /// </summary>
        bool HasJoin { get; }

        /// <summary>
        /// Gets a value indicating whether the query contains an order clause.
        /// </summary>
        bool HasOrder { get; }

        /// <summary>
        /// Removes the group by clause from the query.
        /// </summary>
        void ClearGroupBy();

        /// <summary>
        /// Removes the order clause from the query.
        /// </summary>
        void ClearOrder();

        /// <summary>
        /// Sets an ascending order clause, replacing any existing one.
        /// </summary>
        /// <param name="orderBy">The order by expression.</param>
        void OrderBy(string orderBy);

        /// <summary>
        /// Sets a descending order clause, replacing any existing one.
        /// </summary>
        /// <param name="orderBy">The order by expression.</param>
        void OrderByDescending(string orderBy);

        /// <summary>
        /// Sets a random order clause, replacing any existing one.
        /// </summary>
        void OrderByRandom();

        /// <summary>
        /// Marks the query as a select statement.
        /// </summary>
        void Select();

        /// <summary>
        /// Sets the selector clause of the query.
        /// </summary>
        /// <param name="selector">The selector expression.</param>
        void Selector(string selector);

        /// <summary>
        /// Sets the selector clause of the query to a single column.
        /// </summary>
        /// <param name="table">The name of the table the column belongs to.</param>
        /// <param name="column">The name of the column.</param>
        /// <param name="schema">The schema the table belongs to.</param>
        void Selector(string table, string column, string schema);

        /// <summary>
        /// Appends an expression to the selector clause of the query.
        /// </summary>
        /// <param name="select">The selector expression to add.</param>
        void AddSelector(string select);

        /// <summary>
        /// Inserts an expression at the beginning of the selector clause of the query.
        /// </summary>
        /// <param name="select">The selector expression to insert.</param>
        void InsertSelector(string select);

        /// <summary>
        /// Marks the selector clause as returning distinct rows.
        /// </summary>
        void Distinct();

        /// <summary>
        /// Gets a value indicating whether the query has a paging clause (skip or take).
        /// </summary>
        bool HasPaging { get; }

        /// <summary>
        /// Sets the number of rows to skip.
        /// </summary>
        /// <param name="skip">The skip expression.</param>
        void Skip(string skip);

        /// <summary>
        /// Sets the number of rows to take.
        /// </summary>
        /// <param name="take">The take expression.</param>
        void Take(string take);

        /// <summary>
        /// Sets the table the query operates on.
        /// </summary>
        /// <param name="table">The name of the table.</param>
        /// <param name="alias">The alias of the table.</param>
        /// <param name="schema">The schema the table belongs to.</param>
        void Table(string table, string alias, string schema);

        /// <summary>
        /// Sets the from clause of the query.
        /// </summary>
        /// <param name="from">The from expression.</param>
        void From(string from);

        /// <summary>
        /// Appends an additional ascending order clause to the query.
        /// </summary>
        /// <param name="orderBy">The order by expression.</param>
        void ThenOrderBy(string orderBy);

        /// <summary>
        /// Appends an additional descending order clause to the query.
        /// </summary>
        /// <param name="orderBy">The order by expression.</param>
        void ThenOrderByDescending(string orderBy);

        /// <summary>
        /// Appends an additional random order clause to the query.
        /// </summary>
        void ThenOrderByRandom();

        /// <summary>
        /// Sets the having clause of the query.
        /// </summary>
        /// <param name="having">The having expression.</param>
        void Having(string having);

        /// <summary>
        /// Sets the group by clause of the query.
        /// </summary>
        /// <param name="groupBy">The group by expression.</param>
        void GroupBy(string groupBy);

        /// <summary>
        /// Appends a trailing fragment to the generated SQL.
        /// </summary>
        /// <param name="trail">The trailing SQL fragment.</param>
        void Trail(string trail);

        /// <summary>
        /// Removes the trailing fragment from the generated SQL.
        /// </summary>
        void ClearTrail();

        /// <summary>
        /// Builds the SQL string for the current query.
        /// </summary>
        /// <returns>The generated SQL statement.</returns>
        string ToSqlString();

        /// <summary>
        /// Combines the specified clause with the existing where clause using a logical AND.
        /// </summary>
        /// <param name="clause">The clause to add.</param>
        void WhereAnd(string clause);

        /// <summary>
        /// Combines the specified clause with the existing where clause using a logical OR.
        /// </summary>
        /// <param name="clause">The clause to add.</param>
        void WhereOr(string clause);

        /// <summary>
        /// Creates a deep copy of this builder.
        /// </summary>
        /// <returns>A new <see cref="ISqlBuilder"/> that is a copy of this instance.</returns>
        ISqlBuilder Clone();

        /// <summary>
        /// Gets the individual selector expressions of the query.
        /// </summary>
        /// <returns>The selector expressions.</returns>
        IEnumerable<string> GetSelectors();

        /// <summary>
        /// Gets the individual order expressions of the query.
        /// </summary>
        /// <returns>The order expressions.</returns>
        IEnumerable<string> GetOrders();

        /// <summary>
        /// Adds a join between two tables to the query.
        /// </summary>
        /// <param name="type">The type of join to add.</param>
        /// <param name="table">The name of the table to join.</param>
        /// <param name="onTable">The name of the table that holds the join column.</param>
        /// <param name="onColumn">The name of the column on the joined table.</param>
        /// <param name="toTable">The name of the table being joined to.</param>
        /// <param name="toColumn">The name of the column on the table being joined to.</param>
        /// <param name="schema">The schema the tables belong to.</param>
        /// <param name="alias">An optional alias for the joined table.</param>
        /// <param name="toAlias">An optional alias for the table being joined to.</param>
        void Join(JoinType type, string table, string onTable, string onColumn, string toTable, string toColumn, string schema, string alias = null, string toAlias = null);

        /// <summary>
        /// Combines the specified clause with the existing having clause using a logical AND.
        /// </summary>
        /// <param name="having">The having clause to add.</param>
        void HavingAnd(string having);

        /// <summary>
        /// Combines the specified clause with the existing having clause using a logical OR.
        /// </summary>
        /// <param name="having">The having clause to add.</param>
        void HavingOr(string having);
    }
}
