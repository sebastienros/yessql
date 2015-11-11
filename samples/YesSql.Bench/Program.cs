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
            });

            _store.CreateSchema().Wait();

            using (var session = _store.CreateSession())
            {
                var user = session.QueryAsync<User>().FirstOrDefault().Result;
                Assert.Null(user);

                var bill = new User
                {
                    Name = "Bill"
                };

                Assert.True(bill.Id == 0);
                session.Save(bill);
                Assert.True(bill.Id > 0);

                var newBill = session.GetAsync<User>(bill.Id).Result;

                Assert.NotNull(newBill);
                Assert.Same(bill, newBill);
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
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
