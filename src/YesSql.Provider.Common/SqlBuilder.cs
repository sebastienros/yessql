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
        protected string _selector;

        protected StringBuilder _join;
        protected string _where;
        protected string _order;

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
            if (_join == null)
            {
                _join = new StringBuilder();
            }

            _join.Append("inner join ").Append(_dialect.QuoteForTableName(_tablePrefix + table))
                .Append(" on ").Append(_dialect.QuoteForTableName(_tablePrefix + onTable))
                .Append(".").Append(_dialect.QuoteForColumnName(onColumn))
                .Append(" = ").Append(_dialect.QuoteForTableName(_tablePrefix + toTable))
                .Append(".").Append(_dialect.QuoteForColumnName(toColumn)).Append(" ");
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
            if (_where == null)
            {
                _where = where;
            }
            else
            {
                _where += " and " + where;
            }
        }

        public virtual void OrderBy(string orderBy)
        {
            _order = orderBy;
        }

        public virtual void OrderByDescending(string orderBy)
        {
            _order = orderBy + " desc";
        }

        public virtual void ThenOrderBy(string orderBy)
        {
            _order += ", " + orderBy;
        }

        public virtual void ThenOrderByDescending(string orderBy)
        {
            _order += ", " + orderBy + " desc";
        }

        public string Trail { get; set; }

        public abstract string ToSqlString(ISqlDialect dialect, bool ignoreOrderBy = false);
       
    }
}
