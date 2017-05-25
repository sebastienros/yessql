using System;
using System.Text;
using YesSql.Sql;

namespace YesSql.Provider.Sqlite
{
    public class SqliteSqlBuilder : SqlBuilder
    {
        public SqliteSqlBuilder(string tablePrefix, ISqlDialect dialect) : base(tablePrefix, dialect)
        {

        }
        public override string ToSqlString(ISqlDialect dialect, bool ignoreOrderBy = false)
        {
            if (_clause == "select")
            {
                if ((_skip != 0 || _count != 0))
                {
                    dialect.Page(this, _skip, _count);
                }

                var sb = new StringBuilder();
                sb
                    .Append(_clause).Append(" ").Append(_selector)
                    .Append(" from ").Append(_dialect.QuoteForTableName(_tablePrefix + _table));

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
