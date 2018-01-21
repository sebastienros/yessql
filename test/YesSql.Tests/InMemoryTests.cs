using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.Sqlite;
using YesSql.Sql;

namespace YesSql.Tests
{
    public class InMemoryTests : CoreTests
    {
        public InMemoryTests()
        {
            _store = new Store(new Configuration().UseInMemory().SetTablePrefix(TablePrefix));


            CleanDatabase(false);
            CreateTables();
        }

        protected override void OnCleanDatabase(SchemaBuilder builder, ISession session)
        {
            base.OnCleanDatabase(builder, session);

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

        [Fact(Skip = "Shared connection can't be used concurrently")]
        public override Task ShouldGateQuery()
        {
            return base.ShouldGateQuery();
        }
    }
}
