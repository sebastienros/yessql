using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YesSql.Core.Sql
{
    public class SqlBuilder
    {
        private string _clause;
        private string _table;
        private string _selector;
        private List<string> _join = new List<string>();
        private List<string> _where = new List<string>();
        private List<string> _order = new List<string>();

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
            _join.Add($"inner join [{TablePrefix}{table}] on [{TablePrefix}{onTable}].{onColumn} = [{TablePrefix}{toTable}].{toColumn}");
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

        public string FormatColumn(string table, string column)
        {
            return "[" + TablePrefix + table + "]." + column;
        }

        public void Where(string where)
        {
            _where.Clear();
            _where.Add(where);
        }

        public void WhereAlso(string where)
        {
            _where.Add(where);
        }

        public void OrderBy(string orderBy)
        {
            _order.Clear();
            _order.Add(orderBy);
        }

        public void OrderByDescending(string orderBy)
        {
            _order.Clear();
            _order.Add(orderBy + " desc");
        }

        public void ThenOrderBy(string orderBy)
        {
            _order.Add(orderBy);
        }

        public void ThenOrderByDescending(string orderBy)
        {
            _order.Add(orderBy + " desc");
        }

        public string ToSqlString(ISqlDialect dialect)
        {
            if (_clause == "select")
            {
                var sb = new StringBuilder();
                sb.Append($"{_clause} {_selector} from [{TablePrefix}{_table}]");

                if (_join.Any())
                {
                    sb.AppendFormat(" {0}", String.Join(" ", _join));
                }

                if (_where.Any())
                {
                    sb.AppendFormat(" where {0}", String.Join(" and ", _where));
                }

                if (_order.Any())
                {
                    sb.AppendFormat(" order by {0}", String.Join(", ", _order));
                }

                var sql = sb.ToString();

                if (_skip != 0 || _count != 0)
                {
                    sql = dialect.Page(sql, _skip, _count);
                }

                return sql;
            }

            return "";
        } 
    }
}
