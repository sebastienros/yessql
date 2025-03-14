using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using YesSql.Provider.PostgreSql;

namespace YesSql.Tests
{
    // Docker command
    // docker run --name postgresql -e POSTGRES_USER=root -e POSTGRES_PASSWORD=Password12! -e POSTGRES_DB=yessql -d -p 5432:5432 postgres:11
    public class PostgreSqlLegacyIdentityTests : PostgreSqlTests
    {
        public PostgreSqlLegacyIdentityTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            var config = new Configuration()
                .UsePostgreSql(ConnectionStringBuilder.ConnectionString)
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                .SetIdentityColumnSize(IdentityColumnSize.Int32);

            config.EnableThreadSafetyChecks = true;

            return config;
        }

        [Fact(Skip = "Skip to make test faster in this configuration")]
        public override Task ShouldGateQuery()
        {
            return base.ShouldGateQuery();
        }
    }
}
