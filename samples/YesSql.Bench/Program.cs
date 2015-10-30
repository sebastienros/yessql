using System.Data.SQLite;
using Xunit;
using YesSql.Core.Services;
using YesSql.Core.Storage.InMemory;
using YesSql.Tests.Indexes;

namespace Bench
{
    class Program
    {
        static void Main(string[] args)
        {
            var _store = new Store(cfg =>
            {
                cfg.ConnectionFactory = new DbConnectionFactory<SQLiteConnection>(@"Data Source=:memory:", true);
                cfg.DocumentStorageFactory = new InMemoryDocumentStorageFactory();

                cfg.Migrations.Add(builder => builder
                    .CreateMapIndexTable(nameof(PersonByName), table => table
                        .Column<string>("Name")
                    )
                    .CreateReduceIndexTable(nameof(ArticlesByDay), table => table
                        .Column<int>("Count")
                        .Column<int>("DayOfYear")
                    )
                );
            });

            _store.RegisterIndexes<PersonIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var bill = new
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Address = new
                    {
                        Street = "1 Microsoft Way",
                        City = "Redmond"
                    }
                };

                session.Save(bill);
            }

            using (var session = _store.CreateSession())
            {
                dynamic person = session.QueryAsync().Any().FirstOrDefault().Result;

                Assert.NotNull(person);
                Assert.Equal("Bill", (string)person.Firstname);
                Assert.Equal("Gates", (string)person.Lastname);

                Assert.NotNull(person.Address);
                Assert.Equal("1 Microsoft Way", (string)person.Address.Street);
                Assert.Equal("Redmond", (string)person.Address.City);
            }
        }
    }
}
