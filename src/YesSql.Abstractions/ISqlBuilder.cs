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
        void Skip(int skip);
        void Table(string table);
        void Take(int take);
        void ThenOrderBy(string orderBy);
        void ThenOrderByDescending(string orderBy);
        void Having(string having);
        void GroupBy(string groupBy);
        void Trail(string trail);
        void ClearTrail();
        string ToSqlString(ISqlDialect dialect, bool ignoreOrderBy = false);
        void WhereAlso(string where);
    }
}