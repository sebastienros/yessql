using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YesSql.Provider.SqlServer;
using YesSql.Provider.Sqlite;
using YesSql.Provider.PostgreSql;
using YesSql.Sql;

namespace YesSql.Samples.Gating
{
    class Program
    {
        static void Main(string[] args)
        {
            // Uncomment to use SQL Server

            var configuration = new Configuration()
                    .UseSqlServer(@"Data Source =.; Initial Catalog = yessql; Integrated Security = True")
                    .SetTablePrefix("Gating");

            // Uncomment to use Sqlite

            //var store = new Store(
            //    new Configuration()
            //        .UseSqLite("Data Source=yessql.db;Cache=Shared")
            //    );

            // Uncomment to use PostgreSql

            //var store = new Store(
            //    new Configuration()
            //        .UsePostgreSql(@"Server=localhost;Port=5432;Database=yessql;User Id=root;Password=Password12!;Maximum Pool Size=1024;NoResetOnClose=true;Enlist=false;Max Auto Prepare=200")
            //        .SetTablePrefix("Gating")
            //    );

            // Uncomment to disable gating

            // store.Configuration.DisableQueryGating();

            try
            {
                using (var connection = configuration.ConnectionFactory.CreateConnection())
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction(configuration.IsolationLevel))
                    {
                        new SchemaBuilder(configuration, transaction)
                        .DropMapIndexTable(nameof(PersonByName))
                        .DropTable("Identifiers")
                        .DropTable("Document");

                        transaction.Commit();
                    }
                }
            }
            catch { }

            using (var connection = configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(configuration, transaction)
                    .CreateMapIndexTable(nameof(PersonByName), column => column
                        .Column<string>(nameof(PersonByName.SomeName))
                    );

                    transaction.Commit();
                }
            }

            var store = StoreFactory.CreateAsync(configuration).GetAwaiter().GetResult();

            store.RegisterIndexes<PersonIndexProvider>();

            Console.WriteLine("Creating content...");
            using (var session = store.CreateSession())
            {
                for (var i = 0; i < 10000; i++)
                {
                    session.Save(new Person() { Firstname = "Steve" + i });
                }
            }

            // Warmup
            Console.WriteLine("Warming up...");
            using (var session = store.CreateSession())
            {
                Task.Run(async () =>
                {
                    for (var i = 0; i < 500; i++)
                    {
                        await session.Query().For<Person>().With<PersonByName>(x => x.SomeName.StartsWith("Steve100")).ListAsync();
                        await session.Query().For<Person>().With<PersonByName>(x => x.SomeName == "Steve200").ListAsync();
                    }
                }).GetAwaiter().GetResult();
            }

            var concurrency = 20;
            var counter = 0;
            var MaxTransactions = 50000;
            var stopping = false;

            var tasks = Enumerable.Range(1, concurrency).Select(i => Task.Run(async () =>
            {
                Console.WriteLine($"Starting thread {i}");

                await Task.Delay(100);

                while (!stopping && Interlocked.Add(ref counter, 1) < MaxTransactions)
                {
                    using (var session = store.CreateSession())
                    {
                        await session.Query().For<Person>().With<PersonByName>(x => x.SomeName.StartsWith("Steve100")).ListAsync();
                        await session.Query().For<Person>().With<PersonByName>(x => x.SomeName == "Steve").ListAsync();
                        await session.Query().For<Person>().With<PersonByName>().Where(x => x.SomeName == "Steve").ListAsync();
                    }
                }
            })).ToList();

            tasks.Add(Task.Delay(TimeSpan.FromSeconds(3)));

            Task.WhenAny(tasks).GetAwaiter().GetResult();

            // Flushing tasks
            stopping = true;
            Task.WhenAll(tasks).GetAwaiter().GetResult();
            stopping = false;

            Console.WriteLine(counter);
        }
    }
}
