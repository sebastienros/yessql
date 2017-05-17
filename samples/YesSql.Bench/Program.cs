using System.Threading.Tasks;
using Xunit;
using YesSql;
using YesSql.Indexes;
using YesSql.Provider.SqlServer;
using YesSql.Sql;

namespace Bench
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            var store = new Store(
                new Configuration()
                    .UseSqlServer(@"Data Source =.; Initial Catalog = yessql; Integrated Security = True")
                    .SetTablePrefix("Bench")
                );

            await store.InitializeAsync();

            using (var session = store.CreateSession())
            {
                var builder = new SchemaBuilder(session);

                builder.CreateMapIndexTable(nameof(UserByName), c => c
                    .Column<string>("Name")
                    .Column<bool>("Adult")
                    .Column<int>("Age")
                );
            }

            store.RegisterIndexes<UserIndexProvider>();

            using (var session = store.CreateSession())
            {
                var user = await session.QueryAsync<User>().FirstOrDefault();
                Assert.Null(user);

                var bill = new User
                {
                    Name = "Bill",
                    Adult = true,
                    Age = 1
                };


                session.Save(bill);

            }

            using (var session = store.CreateSession())
            {
                var user = await session.QueryAsync<User, UserByName>().Where(x => x.Adult == true).FirstOrDefault();
                Assert.NotNull(user);

                user = await session.QueryAsync<User, UserByName>().Where(x => x.Age == 1).FirstOrDefault();
                Assert.NotNull(user);

                user = await session.QueryAsync<User, UserByName>().Where(x => x.Age == 1 && x.Adult).FirstOrDefault();
                Assert.NotNull(user);

                user = await session.QueryAsync<User, UserByName>().Where(x => x.Name.StartsWith("B")).FirstOrDefault();
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
