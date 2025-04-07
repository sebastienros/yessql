using System.Threading.Tasks;
using YesSql;
using YesSql.Indexes;
using YesSql.Provider.SqlServer;
using YesSql.Sql;

namespace Bench
{
    sealed class Program
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

            await using (var connection = configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync(configuration.IsolationLevel);
                var builder = new SchemaBuilder(configuration, transaction);

                await builder.CreateMapIndexTableAsync<UserByName>(c => c
                    .Column<string>("Name")
                    .Column<bool>("Adult")
                    .Column<int>("Age")
                );

                await transaction.CommitAsync();
            }

            var store = await StoreFactory.CreateAndInitializeAsync(configuration);
            store.RegisterIndexes<UserIndexProvider>();

            await using (var session = store.CreateSession())
            {
                var user = await session.Query<User>().FirstOrDefaultAsync();

                var bill = new User
                {
                    Name = "Bill",
                    Adult = true,
                    Age = 1
                };

                await session.SaveAsync(bill);
                await session.SaveChangesAsync();
            }

            await using (var session = store.CreateSession())
            {
                var user = await session.Query<User, UserByName>().Where(x => x.Adult == true).FirstOrDefaultAsync();

                user = await session.Query<User, UserByName>().Where(x => x.Age == 1).FirstOrDefaultAsync();

                user = await session.Query<User, UserByName>().Where(x => x.Age == 1 && x.Adult).FirstOrDefaultAsync();

                user = await session.Query<User, UserByName>().Where(x => x.Name.StartsWith('B')).FirstOrDefaultAsync();
            }
        }
    }

    public class User
    {
        public long Id { get; set; }
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
