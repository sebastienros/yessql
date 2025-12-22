using Xunit;
using YesSql.Provider.SqlServer;
using YesSql.Tests.Fixtures;

namespace YesSql.Tests
{
    /// <summary>
    /// Run all tests with SQL Server 2019 using Testcontainers.
    /// </summary>
    [Collection("SqlServer2019")]
    public class SqlServer2019Tests : SqlServerTests<SqlServer2019ContainerFixture>
    {
        public SqlServer2019Tests(SqlServer2019ContainerFixture fixture, ITestOutputHelper output) : base(fixture, output)
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
