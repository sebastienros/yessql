using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.Sqlite;
using YesSql.Sql;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    /// <summary>
    /// Run all tests with a SqlServer document storage
    /// </summary>
    public class SqliteTests : CoreTests
    {
        private TemporaryFolder _tempFolder;

        public SqliteTests()
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            _tempFolder = new TemporaryFolder();
            var connectionString = @"Data Source=" + _tempFolder.Folder + "yessql.db;Cache=Shared";

            return new Configuration()
                .UseSqLite(connectionString)
                .SetTablePrefix(TablePrefix)
                .UseDefaultIdGenerator()
                ;
        }

        protected override void OnCleanDatabase(SchemaBuilder builder, DbTransaction transaction)
        {
            base.OnCleanDatabase(builder, transaction);

            try
            {
                builder.DropTable("Content");
            }
            catch { }

            try
            {
                builder.DropTable("Collection1_Content");
            }
            catch { }
        }

        [Fact(Skip = "ReadCommitted is not supported by Sqlite")]
        public override Task ShouldReadCommittedRecords()
        {
            return base.ShouldReadCommittedRecords();
        }

        [Fact(Skip = "Sqlite doesn't support concurrent writers")]
        public override Task ShouldReadUncommittedRecords()
        {
            return base.ShouldReadUncommittedRecords();
        }

        [Fact]
        public async Task ShouldSeedExistingIds()
        {
            using (_tempFolder = new TemporaryFolder())
            {
                var connectionString = @"Data Source=" + _tempFolder.Folder + "yessql.db;Cache=Shared";

                var store1 = await StoreFactory.CreateAsync(new Configuration().UseSqLite(connectionString).SetTablePrefix(TablePrefix).UseDefaultIdGenerator());

                using (var session1 = store1.CreateSession())
                {
                    var p1 = new Person { Firstname = "Bill" };

                    session1.Save(p1);

                    Assert.Equal(1, p1.Id);
                }

                var store2 = await StoreFactory.CreateAsync(new Configuration().UseSqLite(connectionString).SetTablePrefix(TablePrefix).UseDefaultIdGenerator());

                using (var session2 = store2.CreateSession())
                {
                    var p2 = new Person { Firstname = "Bill" };

                    session2.Save(p2);

                    Assert.Equal(2, p2.Id);
                }
            }
        }

        [Fact(Skip = "Locking prevents Sqlite from concurrency issues")]
        public override Task ShouldHandleConcurrency()
        {
            return base.ShouldHandleConcurrency();
        }
    }
}
