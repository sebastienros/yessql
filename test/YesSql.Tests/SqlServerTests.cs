using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.SqlServer;
using YesSql.Sql;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    public class SqlServerTests : CoreTests
    {
        public static string ConnectionString => Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING") ?? @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True";

        public SqlServerTests()
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseSqlServer(ConnectionString)
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                ;
        }

        protected override void OnCleanDatabase(SchemaBuilder builder, DbTransaction transaction)
        {
            base.OnCleanDatabase(builder, transaction);

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

        [Fact]
        public async Task ShouldSeedExistingIds()
        {
            var configuration = new Configuration().UseSqlServer(ConnectionString).SetTablePrefix("Store1").UseBlockIdGenerator();

            using (var connection = configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    var builder = new SchemaBuilder(configuration, transaction, throwOnError: false);

                    builder.DropTable(configuration.TableNameConvention.GetDocumentTable(""));
                    builder.DropTable("Identifiers");

                    transaction.Commit();
                }
            }

            var store1 = await StoreFactory.CreateAsync(configuration);

            using (var session1 = store1.CreateSession())
            {
                var p1 = new Person { Firstname = "Bill" };

                session1.Save(p1);

                Assert.Equal(1, p1.Id);
            }

            var store2 = await StoreFactory.CreateAsync(new Configuration().UseSqlServer(ConnectionString).SetTablePrefix("Store1").UseBlockIdGenerator());

            using (var session2 = store2.CreateSession())
            {
                var p2 = new Person { Firstname = "Bill" };

                session2.Save(p2);

                Assert.Equal(21, p2.Id);

            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("Collection1")]
        public async Task ShouldGenerateIdsWithConcurrentStores(string collection)
        {
            var configuration = new Configuration().UseSqlServer(ConnectionString).SetTablePrefix("Store1").UseBlockIdGenerator();

            using (var connection = configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(configuration, transaction, throwOnError: false);

                    builder.DropTable(configuration.TableNameConvention.GetDocumentTable(""));
                    builder.DropTable("Identifiers");

                    transaction.Commit();
                }
            }

            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var man = new ManualResetEventSlim();
            var concurrency = 8;
            var MaxTransactions = 5000;
            long lastId = 0;
            var results = new bool[2 * MaxTransactions];

            var tasks = Enumerable.Range(1, concurrency).Select(i => Task.Run(async () =>
            {
                var store1 = await StoreFactory.CreateAsync(configuration);
                await store1.InitializeCollectionAsync(collection);
                long taskId;
                man.Wait();

                while (!cts.IsCancellationRequested)
                {
                    lastId = taskId = store1.Configuration.IdGenerator.GetNextId(collection);

                    if (taskId > MaxTransactions)
                    {
                        break;
                    }

                    Assert.False(results[taskId], $"Found duplicate identifier: '{taskId}'");
                    results[taskId] = true;
                }
            })).ToList();

            await Task.Delay(1000);
            man.Set();
            await Task.WhenAll(tasks);

            Assert.True(lastId >= MaxTransactions, $"lastId: {lastId}");
        }

        [Fact]
        public async Task LastOneInWins()
        {
            // Shows that there's an existing document created at some point in the past.            
            using (var session = _store.CreateSession())
            {
                var bill = new Person
                {
                    Firstname = "Bill",
                    Lastname = "Gates",
                    Age = 18
                };

                session.Save(bill);
            }

            // ** The document is at version 1 **

            var user_b_saved = new ManualResetEventSlim(false);

            var user_a = Task.Run(async () =>
            {
                PersonViewModel vm;

                {
                    // 1. User-A creates a Session-A, loads the document, populates the web form, and then the Session - A is disposed. The request is complete.
                    using var session = _store.CreateSession();
                    var document = await session.Query<Person>().FirstOrDefaultAsync();

                    vm = new PersonViewModel(document); // ** There's no public API that provides a way to capture the document's Version right here.
                }

                // person is now being editing by User-A in a web form.
                vm.Firstname = "William";
                vm.Anonymous = false;

                user_b_saved.Wait();

                {
                    // 4. User-A submits form with Values-B two minutes later, creates Session-D, updates the documents, saves, and then Session-D is disposed. The request is complete.
                    using var session = _store.CreateSession();

                    var document = await session.Query<Person>().FirstOrDefaultAsync();

                    document.Firstname = vm.Firstname;
                    document.Lastname = vm.Lastname;
                    document.Age = vm.Age;
                    document.Anonymous = vm.Anonymous;

                    session.Save(document); // ** There's no public API that provides a way to specify a document's Version right here to use in a concurrency check.
                    await session.CommitAsync();
                }
            });

            var user_b = Task.Run(async () =>
            {
                PersonViewModel vm;

                {
                    // 2. User-B creates a Session-B, loads the document, populates the web form, and then the Session-C is disposed. The request is complete.
                    using var session = _store.CreateSession();
                    var document = await session.Query<Person>().FirstOrDefaultAsync();

                    vm = new PersonViewModel(document); // ** There's no public API that provides a way to capture the document's Version right here.
                }

                // person is now being editing by User-B in a web form.
                vm.Age = 13;
                vm.Anonymous = true;

                {
                    // 4. User-B submits form with Values-A, creates Session-C, updates the documents, saves, and then Session-C is disposed. The request is complete.
                    using var session = _store.CreateSession();

                    var document = await session.Query<Person>().FirstOrDefaultAsync();

                    document.Firstname = vm.Firstname;
                    document.Lastname = vm.Lastname;
                    document.Age = vm.Age;
                    document.Anonymous = vm.Anonymous;
                    
                    session.Save(document); // ** There's no public API that provides a way to specify a document's Version right here to use in a concurrency check.
                    await session.CommitAsync();
                }

                user_b_saved.Set();
            });

            await Task.WhenAll(user_a, user_b);

            // 6. Document contains Value-B (from User-A), but that's only because User-A was the "last one in", but not because this *should* be the state of the Document.

            // Since User-A and User-B made changes to the person, but what *should* be the values for this entity based on business rules?
            
            // Should it look like User-A's changes:
            //     Person { FirstName = "William", LastName = "Gates", Age = 18, Anonymous = false }
            // or should it look like User-B's changes:
            //     Person { FirstName = "Bill", LastName = "Gates", Age = 13, Anonymous = true }

            throw new Exception("Inconclusive");
        }

        public class PersonViewModel
        {
            public PersonViewModel(Person person)
            {
                Id = person.Id;
                Firstname = person.Firstname;
                Lastname = person.Lastname;
                Age = person.Age;
                Anonymous = person.Anonymous;
            }

            public int Id { get; set; }
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public int Age { get; set; }
            public bool Anonymous { get; set; }
        }
    }
}
