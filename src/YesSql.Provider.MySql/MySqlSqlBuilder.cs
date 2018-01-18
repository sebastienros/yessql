using YesSql.Sql;

namespace YesSql.Provider.MySql
{
    public class MySqlSqlBuilder : SqlBuilder
    {
        public MySqlSqlBuilder(string tablePrefix, ISqlDialect dialect) : base(tablePrefix, dialect)
        {
        }
    }
}
