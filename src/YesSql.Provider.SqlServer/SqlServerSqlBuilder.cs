using YesSql.Sql;

namespace YesSql.Provider.SqlServer
{
    public class SqlServerSqlBuilder : SqlBuilder
    {
        public SqlServerSqlBuilder(string tablePrefix, ISqlDialect dialect) : base(tablePrefix, dialect)
        {
        }
       
    }
}
