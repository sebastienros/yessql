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

            _join.Append("inner join ").Append("[").Append(TablePrefix).Append(table)
                .Append("] on [").Append(TablePrefix).Append(onTable)
                .Append("].[").Append(onColumn).Append("]")
                .Append(" = [").Append(TablePrefix).Append(toTable)
                .Append("].[").Append(toColumn).Append("] ");
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
                column = "[" + column + "]";
            }

            return "[" + TablePrefix + table + "]." + column;
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
                if (_skip != 0 || _count != 0)
                {
                    dialect.Page(this, _skip, _count);
                }

                var sb = new StringBuilder();
                sb
                    .Append(_clause).Append(" ").Append(_selector)
                    .Append(" from [").Append(TablePrefix).Append(_table).Append("]");

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

                return sb.ToString();
            }

            return "";
        }
    }
}
