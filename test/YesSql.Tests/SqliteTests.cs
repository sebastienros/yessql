using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.Sqlite;
using YesSql.Sql;
using YesSql.Tests.CompiledQueries;
using YesSql.Tests.Indexes;
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
            _tempFolder = new TemporaryFolder();
            var connectionString = @"Data Source=" + _tempFolder.Folder + "yessql.db;Cache=Shared";

            _store = new Store(new Configuration().UseSqLite(connectionString).SetTablePrefix(TablePrefix));


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

        [Fact]
        public async Task OrderQueryWithCollation()
        {
            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var seb1 = new Person
                {
                    Firstname = "Sebastien",
                    Lastname = "Ros",
                    Age = 45
                };

                var hisham1 = new Person
                {
                    Firstname = "Hisham",
                    Lastname = "Bin Ateya",
                    Age = 34
                };

                var hisham2 = new Person
                {
                    Firstname = "hisham",
                    Lastname = "Bin Ateya",
                    Age = 34
                };

                var hao = new Person
                {
                    Firstname = "Hao",
                    Lastname = "Kung",
                    Age = 37
                };

                var seb2 = new Person
                {
                    Firstname = "sebastien",
                    Lastname = "Ros",
                    Age = 45
                };

                session.Save(seb1);
                session.Save(hisham1);
                session.Save(hisham2);
                session.Save(hao);
                session.Save(seb2);

            }

            using (var session = _store.CreateSession())
            {
                var result = await session.ExecuteQuery(new PersonOrderedAscByNameQuery()).ListAsync();
                var names = result.Select(p => p.Firstname);
                Assert.Equal(new[] { "Hao", "Hisham", "hisham", "Sebastien", "sebastien" }, names);
            }
        }
    }
}
