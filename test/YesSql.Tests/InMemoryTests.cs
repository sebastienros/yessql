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
            _store = new Store(new Configuration().UseInMemory());


            CleanDatabase();
            CreateTables();
        }

        protected override void OnCleanDatabase(ISession session)
        {
            base.OnCleanDatabase(session);

            var builder = new SchemaBuilder(session);

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
    }
}
