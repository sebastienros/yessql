using System.Threading.Tasks;
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
            var configuration = new Configuration()
                    .UseSqlServer(@"Data Source =.; Initial Catalog = yessql; Integrated Security = True")
                    .SetTablePrefix("Bench");

            using (var connection = configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(configuration, transaction);

                    builder.CreateMapIndexTable(nameof(UserByName), c => c
                        .Column<string>("Name")
                        .Column<bool>("Adult")
                        .Column<int>("Age")
                    );

                    transaction.Commit();
                }
            }

            var store = await StoreFactory.CreateAsync(configuration);
            store.RegisterIndexes<UserIndexProvider>();

            using (var session = store.CreateSession())
            {
                var user = await session.Query<User>().FirstOrDefaultAsync();

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
                var user = await session.Query<User, UserByName>().Where(x => x.Adult == true).FirstOrDefaultAsync();

                user = await session.Query<User, UserByName>().Where(x => x.Age == 1).FirstOrDefaultAsync();

                user = await session.Query<User, UserByName>().Where(x => x.Age == 1 && x.Adult).FirstOrDefaultAsync();

                user = await session.Query<User, UserByName>().Where(x => x.Name.StartsWith("B")).FirstOrDefaultAsync();
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
