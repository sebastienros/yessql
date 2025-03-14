using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using YesSql.Provider.Sqlite;

namespace YesSql.Tests
{
    /// <summary>
    /// Run all tests with a Sqlite document storage
    /// </summary>
    public class SqliteLegacyIdentityTests : SqliteTests
    {
        private TemporaryFolder _tempFolder;

        public SqliteLegacyIdentityTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            _tempFolder = new TemporaryFolder();
            var connectionString = @"Data Source=" + _tempFolder.Folder + "yessql.db;Cache=Shared";

            var config = new Configuration()
                .UseSqLite(connectionString)
                .SetTablePrefix(TablePrefix)
                .UseDefaultIdGenerator()
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
