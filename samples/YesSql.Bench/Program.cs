using System.Data.SqlClient;
using Xunit;
using YesSql.Core.Services;
using YesSql.Core.Storage.InMemory;

namespace Bench
{
    class Program
    {
        static void Main(string[] args)
        {
            var _store = new Store(cfg =>
            {
                cfg.ConnectionFactory = new DbConnectionFactory<SqlConnection>(@"Data Source =.; Initial Catalog = yessql; Integrated Security = True");
                //cfg.ConnectionFactory = new DbConnectionFactory<SQLiteConnection>(@"Data Source=:memory:", true);
                cfg.DocumentStorageFactory = new InMemoryDocumentStorageFactory();
                cfg.RunDefaultMigration();
            });


            using (var session = _store.CreateSession())
            {
                var user = session.QueryAsync<User>().FirstOrDefault().Result;
                Assert.Null(user);

                var bill = new User
                {
                    Name = "Bill"
                };

                session.Save(bill);
            }

            using (var session = _store.CreateSession())
            {
                var user = session.QueryAsync<User>().FirstOrDefault().Result;
                Assert.NotNull(user);
            }
        }
    }

    public class User
    {
        public string Name { get; set; }
    }
}
