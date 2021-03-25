using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class SqlServer2008RC2Tests : SqlServerTests
    {
        public SqlServer2008RC2Tests()
        {
        }

        protected override SqlServerDialect SqlServerDialect => new SqlServer2008Dialect();
    }
}
