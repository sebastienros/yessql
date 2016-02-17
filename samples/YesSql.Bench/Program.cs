using System.Data.SqlClient;
using Xunit;
using YesSql.Core.Indexes;
using YesSql.Core.Services;
using YesSql.Storage.LightningDB;

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
                cfg.DocumentStorageFactory = new LightningDocumentStorageFactory("db");
                cfg.TablePrefix = "Bench";
            });

            _store.InitializeAsync().Wait();

            using (var session = _store.CreateSession())
            {
                session.ExecuteMigration(x => x.CreateMapIndexTable(nameof(UserByName), c => c
                    .Column<string>("Name")
                    .Column<bool>("Adult")
                    .Column<int>("Age")
                ));
            }

            _store.RegisterIndexes<UserIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var user = session.QueryAsync<User>().FirstOrDefault().Result;
                Assert.Null(user);

                var bill = new User
                {
                    Name = "Bill",
                    Adult = true,
                    Age = 1
                };


                session.Save(bill);
                
            }

            using (var session = _store.CreateSession())
            {
                var user = session.QueryAsync<User, UserByName>().Where(x => x.Adult == true).FirstOrDefault().Result;
                Assert.NotNull(user);

                user = session.QueryAsync<User, UserByName>().Where(x => x.Age == 1).FirstOrDefault().Result;
                Assert.NotNull(user);

                user = session.QueryAsync<User, UserByName>().Where(x => x.Age == 1 && x.Adult).FirstOrDefault().Result;
                Assert.NotNull(user);

                user = session.QueryAsync<User, UserByName>().Where(x => x.Name.StartsWith("B")).FirstOrDefault().Result;
                Assert.NotNull(user);
            }
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Adult { get; set; }
        public int Age { get; set; }
    }

    public class UserByName : MapIndex
    {
        public string Name { get; set; }
        public bool Adult { get; set; }
        public int Age { get; set; }
    }

    public class UserIndexProvider : IndexProvider<User>
    {
        public override void Describe(DescribeContext<User> context)
        {
            context.For<UserByName>()
                .Map(user => new UserByName { Name = user.Name, Adult = user.Adult, Age = user.Age });
        }
    }

}
