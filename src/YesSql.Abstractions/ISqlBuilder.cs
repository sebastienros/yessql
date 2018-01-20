using System.Collections.Generic;

namespace YesSql
{
    public interface ISqlBuilder
    {
        string Clause { get; }
        Dictionary<string, object> Parameters { get; }
        string FormatColumn(string table, string column);
        string GetSelector();
        void InnerJoin(string table, string onTable, string onColumn, string toTable, string toColumn);
        void OrderBy(string orderBy);
        void OrderByDescending(string orderBy);
        void Select();
        void Selector(string selector);
        void Selector(string table, string column);
        void AddSelector(string select);
        void InsertSelector(string select);
        void Skip(string skip);
        void Take(string take);
        void Table(string table);
        void From(string from);
        void ThenOrderBy(string orderBy);
        void ThenOrderByDescending(string orderBy);
        void Having(string having);
        void GroupBy(string groupBy);
        void Trail(string trail);
        void ClearTrail();
        string ToSqlString(bool ignoreOrderBy = false);
        void WhereAlso(string where);
    }
}