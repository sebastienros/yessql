using System.Collections.Generic;

namespace YesSql
{
    /// <summary>
    /// A class implementing this interface is able to create custom SQL queries.
    /// </summary>
    public interface ISqlBuilder
    {
        string Clause { get; }
        Dictionary<string, object> Parameters { get; }
        string FormatColumn(string table, string column, string schema, bool isAlias = false);
        string FormatTable(string table, string schema);
        string GetSelector();
        bool HasJoin { get; }
        bool HasOrder { get; }
        void ClearGroupBy();
        void ClearOrder();
        void OrderBy(string orderBy);
        void OrderByDescending(string orderBy);
        void OrderByRandom();
        void Select();
        void Selector(string selector);
        void Selector(string table, string column, string schema);
        void AddSelector(string select);
        void InsertSelector(string select);
        void Distinct();
        bool HasPaging { get; }
        void Skip(string skip);
        void Take(string take);
        void Table(string table, string alias, string schema);
        void From(string from);
        void ThenOrderBy(string orderBy);
        void ThenOrderByDescending(string orderBy);
        void ThenOrderByRandom();
        void Having(string having);
        void GroupBy(string groupBy);
        void Trail(string trail);
        void ClearTrail();
        string ToSqlString();
        void WhereAnd(string clause);
        void WhereOr(string clause);
        ISqlBuilder Clone();
        IEnumerable<string> GetSelectors();
        IEnumerable<string> GetOrders();
        void Join(JoinType type, string table, string onTable, string onColumn, string toTable, string toColumn, string schema, string alias = null, string toAlias = null);
        void HavingAnd(string having);
        void HavingOr(string having);
    }
}
