using System;
using System.Collections.Generic;
using System.Text;

namespace YesSql.Core.Sql
{
    public class SqlBuilder
    {
        private string _clause;
        private string _table;
        private string _selector;

        private StringBuilder _join;
        private string _where;
        private string _order;

        private int _skip;
        private int _count;
        private char _openQuoteDialect;
        private char _closeQuoteDialect;

        public Dictionary<string, object> Parameters { get; }
        public string TablePrefix { get; set; }

        public SqlBuilder()
        {
            Parameters = new Dictionary<string, object>();
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
        public void InnerJoin(string table, string onTable, string onColumn, string toTable, string toColumn)
        {
            if (_join == null)
            {
                _join = new StringBuilder();
            }

            _join.Append("inner join ").Append(_openQuoteDialect).Append(TablePrefix).Append(table)
                .Append(_closeQuoteDialect + " on " + _openQuoteDialect).Append(TablePrefix).Append(onTable)
                .Append(_closeQuoteDialect + "." + _openQuoteDialect).Append(onColumn).Append(_closeQuoteDialect)
                .Append(" = " + _openQuoteDialect).Append(TablePrefix).Append(toTable)
                .Append(_closeQuoteDialect + "." + _openQuoteDialect).Append(toColumn).Append(_closeQuoteDialect + " ");
        }

        public void QuoteDialect(char openQuote, char closeQuote)
        {
            _openQuoteDialect = openQuote;
            _closeQuoteDialect = closeQuote;
        }

        public void Select()
        {
            _clause = "select";
        }

        public void Selector(string selector)
        {
            _selector = selector;
        }

        public void Selector(string table, string column)
        {
            _selector = FormatColumn(table, column);
        }

        public string GetSelector()
        {
            return _selector;
        }

        public string FormatColumn(string table, string column)
        {
            if (column != "*")
            {
                column = _openQuoteDialect + column + _closeQuoteDialect;
            }

            return _openQuoteDialect + TablePrefix + table + _closeQuoteDialect + "." + column;
        }

        public void WhereAlso(string where)
        {
            if (_where == null)
            {
                _where = where;
            }
            else
            {
                _where += " and " + where;
            }
        }

        public void OrderBy(string orderBy)
        {
            _order = orderBy;
        }

        public void OrderByDescending(string orderBy)
        {
            _order = orderBy + " desc";
        }

        public void ThenOrderBy(string orderBy)
        {
            _order += ", " + orderBy;
        }

        public void ThenOrderByDescending(string orderBy)
        {
            _order += ", " + orderBy + " desc";
        }

        public string Trail { get; set; }

        public string ToSqlString(ISqlDialect dialect, bool ignoreOrderBy = false)
        {
            if (_clause == "select")
            {
                if ((_skip != 0 || _count != 0) && (dialect is SqlServerDialect || dialect is SqliteDialect))
                {
                    dialect.Page(this, _skip, _count);
                }

                var sb = new StringBuilder();
                sb
                    .Append(_clause).Append(" ").Append(_selector)
                    .Append(" from " + _openQuoteDialect).Append(TablePrefix).Append(_table).Append(_closeQuoteDialect);

                if (_join != null)
                {
                    sb.Append(" ").Append(_join.ToString());
                }

                if (_where != null)
                {
                    sb.Append(" where ").Append(_where);
                }

                if (_order != null && !ignoreOrderBy)
                {
                    sb.Append(" order by ").Append(_order);
                }

                if (!String.IsNullOrEmpty(Trail))
                {
                    sb.Append(" ").Append(Trail);
                }

                if ((_skip != 0 || _count != 0) && dialect is MySqlDialect)
                {
                    _selector = sb.ToString();
                    dialect.Page(this, _skip, _count);
                    sb.Clear();
                    sb.Append(_selector);
                }
                return sb.ToString();
            }

            return "";
        }
    }
}
