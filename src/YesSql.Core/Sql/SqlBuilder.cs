using System;
using System.Collections.Generic;
using YesSql.Utils;

namespace YesSql.Sql
{
    public class SqlBuilder : ISqlBuilder
    {
        protected ISqlDialect _dialect;
        protected string _tablePrefix;

        protected string _clause;
        protected string _table;

        protected List<string> _select;
        protected List<string> _from;
        protected List<string> _join;
        protected List<List<string>> _or;
        protected List<string> _where;
        protected List<string> _group;
        protected List<string> _having;
        protected List<string> _order;
        protected List<string> _trail;
        protected bool _distinct;
        protected string _skip;
        protected string _count;

        protected List<string> SelectSegments => _select ??= new List<string>();
        protected List<string> FromSegments => _from ??= new List<string>();
        protected List<string> JoinSegments => _join ??= new List<string>();
        protected List<List<string>> OrSegments => _or ??= new List<List<string>>();
        protected List<string> WhereSegments => _where ??= new List<string>();
        protected List<string> GroupSegments => _group ??= new List<string>();
        protected List<string> HavingSegments => _having ??= new List<string>();
        protected List<string> OrderSegments => _order ??= new List<string>();
        protected List<string> TrailSegments => _trail ??= new List<string>();

        public Dictionary<string, object> Parameters { get; protected set; } = new Dictionary<string, object>();

        public SqlBuilder(string tablePrefix, ISqlDialect dialect)
        {
            _tablePrefix = tablePrefix;
            _dialect = dialect;
        }

        public string Clause { get { return _clause; } }

        public void Table(string table, string alias = null)
        {
            FromSegments.Clear();
            FromSegments.Add(_dialect.QuoteForTableName(_tablePrefix + table));

            if (!String.IsNullOrEmpty(alias))
            {
                FromSegments.Add(" AS ");
                FromSegments.Add(_dialect.QuoteForTableName(alias));
            }
        }

        public void From(string from)
        {
            FromSegments.Add(from);
        }

        public bool HasPaging => _skip != null || _count != null;

        public void Skip(string skip)
        {
            _skip = skip;
        }

        public void Take(string take)
        {
            _count = take;
        }

        public virtual void InnerJoin(string table, string onTable, string onColumn, string toTable, string toColumn, string alias = null, string toAlias = null)
        {
            // Don't prefix if alias is used
            if (alias != onTable)
            {
                onTable = _tablePrefix + onTable;
            }

            if (toTable != toAlias)
            {
                toTable = _tablePrefix + toTable;
            }

            if (!String.IsNullOrEmpty(toAlias))
            {
                toTable = toAlias;
            }
            
            JoinSegments.Add(" INNER JOIN ");
            JoinSegments.Add(_dialect.QuoteForTableName(_tablePrefix + table));
            if (!String.IsNullOrEmpty(alias))
            {
                JoinSegments.AddRange(new[] { " AS ", _dialect.QuoteForTableName(alias) });
            }
            JoinSegments.AddRange(new[] {
                " ON ", _dialect.QuoteForTableName(onTable), ".", _dialect.QuoteForColumnName(onColumn),
                " = ", _dialect.QuoteForTableName(toTable), ".", _dialect.QuoteForColumnName(toColumn)
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

        public void Distinct()
        {
            _distinct = true;
        }

        public virtual string FormatColumn(string table, string column, bool isAlias = false)
        {
            if (column != "*")
            {
                column = _dialect.QuoteForColumnName(column);
            }

            if (!isAlias)
            {
                table = _tablePrefix + table;
            }

            return _dialect.QuoteForTableName(table) + "." + column;
        }

        public virtual void AndAlso(string where)
        {
            if (String.IsNullOrWhiteSpace(where))
            {
                return;
            }

            if (WhereSegments.Count > 0)
            {
                WhereSegments.Add(" AND ");
            }

            WhereSegments.Add(where);
        }

        public virtual void WhereOr(string where)
        {
            if (String.IsNullOrWhiteSpace(where))
            {
                return;
            }

            if (WhereSegments.Count > 0)
            {
                WhereSegments.Add(" OR ");
            }

            WhereSegments.Add(where);
        }

        public virtual void WhereAnd(string where)
        {
            if (String.IsNullOrWhiteSpace(where))
            {
                return;
            }

            if (WhereSegments.Count > 0)
            {
                WhereSegments.Add(" AND ");
            }

            WhereSegments.Add(where);
        }

        public bool HasJoin => _join != null && _join.Count > 0;

        public bool HasOrder => _order != null && _order.Count > 0;

        public void ClearOrder()
        {
            _order = null;
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

        public virtual void OrderByRandom()
        {
            OrderSegments.Add(_dialect.RandomOrderByClause);
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

        public virtual void ThenOrderByRandom()
        {
            OrderSegments.Add(", ");
            OrderSegments.Add(_dialect.RandomOrderByClause);
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

        public virtual string ToSqlString()
        {
            if (!String.Equals(_clause, "SELECT", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }

            if (_skip != null || _count != null)
            {
                _dialect.Page(this, _skip, _count);
            }

            var sb = new RentedStringBuilder(Store.LargeBufferSize);

            sb.Append("SELECT ");

            if (_distinct)
            {
                sb.Append("DISTINCT ");

                if (_order != null)
                {
                    _select = _dialect.GetDistinctOrderBySelectString(_select, _order);
                }
            }

            foreach (var s in _select)
            {
                sb.Append(s);
            }

            if (_from != null)
            {
                sb.Append(" FROM ");

                foreach (var s in _from)
                {
                    sb.Append(s);
                }
            }

            if (_join != null)
            {
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

            if (_order != null)
            {
                sb.Append(" ORDER BY ");

                foreach (var s in _order)
                {
                    sb.Append(s);
                }
            }

            if (_trail != null)
            {
                foreach (var s in _trail)
                {
                    sb.Append(s);
                }
            }

            var result = sb.ToString();

            sb.Dispose();

            return result;
        }

        public ISqlBuilder Clone()
        {
            var clone = new SqlBuilder(_tablePrefix, _dialect);

            clone._clause = _clause;
            clone._table = _table;

            clone._select = _select == null ? null : new List<string>(_select);
            clone._from = _from == null ? null : new List<string>(_from);
            clone._join = _join == null ? null : new List<string>(_join);
            clone._where = _where == null ? null : new List<string>(_where);
            clone._group = _group == null ? null : new List<string>(_group);
            clone._having = _having == null ? null : new List<string>(_having);
            clone._order = _order == null ? null : new List<string>(_order);
            clone._trail = _trail == null ? null : new List<string>(_trail);
            clone._skip = _skip;
            clone._count = _count;

            clone.Parameters = new Dictionary<string, object>(Parameters);
            return clone;
        }
    }
}
