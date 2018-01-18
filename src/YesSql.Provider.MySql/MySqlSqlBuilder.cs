using System;
using System.Text;
using YesSql.Sql;

namespace YesSql.Provider.MySql
{
    public class MySqlSqlBuilder : SqlBuilder
    {
        public MySqlSqlBuilder(string tablePrefix, ISqlDialect dialect) : base(tablePrefix, dialect)
        {
        }

        public override string ToSqlString(ISqlDialect dialect, bool ignoreOrderBy = false)
        {
            if (String.Equals(_clause, "SELECT", StringComparison.OrdinalIgnoreCase))
            {
                var sb = new StringBuilder();
                sb.Append("SELECT ");

                foreach(var s in _select)
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

                if (_skip != 0 || _count != 0)
                {
                    var temp = sb.ToString();
                    dialect.Page(this, _skip, _count);
                    sb.Clear();
                    sb.Append(temp);
                }

                return sb.ToString();
            }

            return "";
        }
    }
}
