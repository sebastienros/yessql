using System;
using System.Collections.Generic;
using System.Text;

namespace YesSql.Sql
{
    public abstract class SqlBuilder : ISqlBuilder
    {
        protected ISqlDialect _dialect;
        protected string _tablePrefix;

        protected string _clause;
        protected string _table;

        protected List<string> _select;
        protected List<string> _join;
        protected List<string> _where;
        protected List<string> _group;
        protected List<string> _having;
        protected List<string> _order;
        protected List<string> _trail;

        protected List<string> SelectSegments => _select = _select ?? new List<string>();
        protected List<string> JoinSegments => _join = _join ?? new List<string>();
        protected List<string> WhereSegments => _where = _where ?? new List<string>();
        protected List<string> GroupSegments => _group = _group ?? new List<string>();
        protected List<string> HavingSegments => _having = _having ?? new List<string>();
        protected List<string> OrderSegments => _order = _order ?? new List<string>();
        protected List<string> TrailSegments => _trail = _trail ?? new List<string>();

        protected int _skip;
        protected int _count;

        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        public SqlBuilder(string tablePrefix, ISqlDialect dialect)
        {
            _tablePrefix = tablePrefix;
            _dialect = dialect;
        }

        public string Clause { get { return _clause; } }

        public void Table(string table)
        {
            _table = table;
        }

        public void Skip(int skip)
        {
            _skip = skip;
        }

        public void Take(int take)
        {
            _count = take;
        }

        public virtual void InnerJoin(string table, string onTable, string onColumn, string toTable, string toColumn)
        {
            JoinSegments.AddRange(new[] {
                "INNER JOIN ", _dialect.QuoteForTableName(_tablePrefix + table),
                " ON ", _dialect.QuoteForTableName(_tablePrefix + onTable), ".", _dialect.QuoteForColumnName(onColumn),
                " = ", _dialect.QuoteForTableName(_tablePrefix + toTable), ".", _dialect.QuoteForColumnName(toColumn), " "
                }
            );
        }
        
        public void Select()
        {
            _clause = "SELECT";
        }

        public void Selector(string selector)
        {
            SelectSegments.Clear();
            SelectSegments.Add(selector);
        }

        public void Selector(string table, string column)
        {
            Selector(FormatColumn(table, column));
        }

        public void AddSelector(string select)
        {
            SelectSegments.Add(select);
        }

        public void InsertSelector(string select)
        {
            SelectSegments.Insert(0, select);
        }

        public string GetSelector()
        {
            if (SelectSegments.Count == 1)
            {
                return SelectSegments[0];
            }
            else
            {
                return string.Join("", SelectSegments);
            }
        }

        public virtual string FormatColumn(string table, string column)
        {
            if (column != "*")
            {
                column = _dialect.QuoteForColumnName(column);
            }

            return _dialect.QuoteForTableName(_tablePrefix + table) + "." + column;
        }

        public virtual void WhereAlso(string where)
        {
            if (WhereSegments.Count > 0)
            {
                WhereSegments.Add(" AND ");
            }

            WhereSegments.Add(where);
        }

        public virtual void OrderBy(string orderBy)
        {
            OrderSegments.Add(orderBy);
        }

        public virtual void OrderByDescending(string orderBy)
        {
            OrderSegments.Add(orderBy);
            OrderSegments.Add(" DESC");
        }

        public virtual void ThenOrderBy(string orderBy)
        {
            OrderSegments.Add(", ");
            OrderSegments.Add(orderBy);
        }

        public virtual void ThenOrderByDescending(string orderBy)
        {
            OrderSegments.Add(", ");
            OrderSegments.Add(orderBy);
            OrderSegments.Add(" DESC");
        }

        public virtual void GroupBy(string orderBy)
        {
            GroupSegments.Add(orderBy);
        }

        public virtual void Having(string orderBy)
        {
            HavingSegments.Add(orderBy);
        }

        public virtual void Trail(string segment)
        {
            TrailSegments.Add(segment);
        }

        public virtual void ClearTrail()
        {
            TrailSegments.Clear();
        }

        public virtual string ToSqlString(ISqlDialect dialect, bool ignoreOrderBy = false)
        {
            if (String.Equals(_clause, "SELECT", StringComparison.OrdinalIgnoreCase))
            {
                if ((_skip != 0 || _count != 0))
                {
                    dialect.Page(this, _skip, _count);
                }

                var sb = new StringBuilder();

                sb.Append("SELECT ");

                foreach (var s in _select)
                {
                    sb.Append(s);
                }

                sb.Append(" FROM ").Append(_dialect.QuoteForTableName(_tablePrefix + _table));

                if (_join != null)
                {
                    sb.Append(" ");

                    foreach (var s in _join)
                    {
                        sb.Append(s);
                    }
                }

                if (_where != null)
                {
                    sb.Append(" WHERE ");

                    foreach (var s in _where)
                    {
                        sb.Append(s);
                    }
                }

                if (_order != null && !ignoreOrderBy)
                {
                    sb.Append(" ORDER BY ");

                    foreach (var s in _order)
                    {
                        sb.Append(s);
                    }
                }

                if (_group != null)
                {
                    sb.Append(" GROUP BY ");

                    foreach (var s in _group)
                    {
                        sb.Append(s);
                    }
                }

                if (_having != null)
                {
                    sb.Append(" HAVING ");

                    foreach (var s in _having)
                    {
                        sb.Append(s);
                    }
                }

                if (_trail != null)
                {
                    sb.Append(" ");

                    foreach (var s in _trail)
                    {
                        sb.Append(s);
                    }
                }

                return sb.ToString();
            }

            return "";
        }       
    }
}
