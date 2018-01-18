using YesSql.Sql;

namespace YesSql.Provider.PostgreSql
{
    public class PostgreSqlSqlBuilder : SqlBuilder
    {
        public PostgreSqlSqlBuilder(string tablePrefix, ISqlDialect dialect) : base(tablePrefix, dialect)
        {
        }
    }
}
