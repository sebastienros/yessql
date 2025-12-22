using Xunit;
using YesSql.Provider.SqlServer;
using YesSql.Tests.Fixtures;

namespace YesSql.Tests
{
    /// <summary>
    /// Run all tests with SQL Server 2017 using Testcontainers.
    /// </summary>
    [Collection("SqlServer2017")]
    public class SqlServer2017Tests : SqlServerTests<SqlServer2017ContainerFixture>
    {
        public SqlServer2017Tests(SqlServer2017ContainerFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
            return new Configuration()
                .UseSqlServer(ConnectionStringBuilder.ConnectionString, "BobaFett")
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                .UseThreadSafetyChecks()
                .SetIdentityColumnSize(IdentityColumnSize.Int64);
        }
    }
}
