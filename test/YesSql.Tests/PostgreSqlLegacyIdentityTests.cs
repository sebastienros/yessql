using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using YesSql.Provider.PostgreSql;

namespace YesSql.Tests
{
//    public class PostgreSqlLegacyIdentityTests : PostgreSqlTests
//    {
//        public PostgreSqlLegacyIdentityTests(ITestOutputHelper output) : base(output)
//        {
//        }

//        public override async Task InitializeAsync()
//        {
//            await PostgreSqlContainer.StartAsync();
//            await base.InitializeAsync();
//        }

//        protected override IConfiguration CreateConfiguration()
//        {
//            return new Configuration()
//                .UsePostgreSql(PostgreSqlContainer.GetConnectionString())
//                .SetTablePrefix(TablePrefix)
//                .UseBlockIdGenerator()
//                .SetIdentityColumnSize(IdentityColumnSize.Int32)
//                ;
//        }

//        [Fact(Skip = "Skip to make test faster in this configuration")]
//        public override Task ShouldGateQuery()
//        {
//            return base.ShouldGateQuery();
//        }

//        public async override Task DisposeAsync() => await PostgreSqlContainer.DisposeAsync();
//    }
}
