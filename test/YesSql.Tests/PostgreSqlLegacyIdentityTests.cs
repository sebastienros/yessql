using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.PostgreSql;
using YesSql.Tests.Fixtures;

namespace YesSql.Tests
{
    /// <summary>
    /// Run all tests with a PostgreSQL document storage using Testcontainers with legacy (Int32) identity.
    /// </summary>
    [Collection("PostgreSql")]
    public class PostgreSqlLegacyIdentityTests : PostgreSqlTests
    {
        public PostgreSqlLegacyIdentityTests(PostgreSqlContainerFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UsePostgreSql(ConnectionStringBuilder.ConnectionString)
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                .UseThreadSafetyChecks()
                .SetIdentityColumnSize(IdentityColumnSize.Int32);
        }

        [Fact(Skip = "Skip to make test faster in this configuration")]
        public override Task ShouldGateQuery()
        {
            return base.ShouldGateQuery();
        }
    }
}
