using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.Sqlite;
using YesSql.Sql;
using YesSql.Tests.CompiledQueries;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

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

        [Fact(Skip = "Shared connection can't be used concurrently")]
        public override Task ShouldRunCompiledQueriesConcurrently()
        {
            return base.ShouldRunCompiledQueriesConcurrently();
        }

        [Fact]
        public async Task ShouldNotLeakExpressionTrees()
        {
            _store.RegisterIndexes<PersonAgeIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Age = 50
                };

                var elon = new Person
                {
                    Firstname = "Elon",
                    Lastname = "Musk",
                    Age = 12
                };

                var eilon = new Person
                {
                    Firstname = "Eilon",
                    Lastname = "Lipton",
                    Age = 12
                };

                session.Save(bill);
                session.Save(elon);
                session.Save(eilon);
            }

            // There can't be more than 1000 parameters in a SQLite query.
            // We ensure that no parameter is duplicated.
            for (var i = 0; i < 1000; i++)
            {
                using (var session = _store.CreateSession())
                {
                    Assert.Equal("Bill", (await session.ExecuteQuery(new PersonByNameOrAgeQuery(50, null)).FirstOrDefaultAsync()).Firstname);
                }
            }
        }
    }
}
