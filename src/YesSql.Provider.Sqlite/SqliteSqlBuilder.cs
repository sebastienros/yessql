using YesSql.Sql;

namespace YesSql.Provider.Sqlite
{
    public class SqliteSqlBuilder : SqlBuilder
    {
        public SqliteSqlBuilder(string tablePrefix, ISqlDialect dialect) : base(tablePrefix, dialect)
        {

        }
       
    }
}
