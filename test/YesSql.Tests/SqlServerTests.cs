using Microsoft.Data.SqlClient;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using YesSql.Provider.SqlServer;
using YesSql.Sql;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    public abstract class SqlServerTests : CoreTests
    {
        public abstract string ConnectionString { get; }
        
        public SqlServerTests(ITestOutputHelper output) : base(output)
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

            var store1 = await StoreFactory.CreateAndInitializeAsync(configuration);

            using (var session1 = store1.CreateSession())
            {
                var p1 = new Person { Firstname = "Bill" };

                session1.Save(p1);

                Assert.Equal(1, p1.Id);
            }

            var store2 = await StoreFactory.CreateAndInitializeAsync(new Configuration().UseSqlServer(ConnectionString).SetTablePrefix("Store1").UseBlockIdGenerator());

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
                var store1 = await StoreFactory.CreateAndInitializeAsync(configuration);
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
        public async Task ThrowsWhenIndexKeyLengthExceeded()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder
                        .DropMapIndexTable<PropertyIndex>();

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                        );

                    builder
                        .AlterTable(nameof(PropertyIndex), table => table
                            .CreateIndex("IDX_Property", "Name"));

                    transaction.Commit();
                }
            }

            _store.RegisterIndexes<PropertyIndexProvider>();

            ISession session = null;
            try
            {
                session = _store.CreateSession();
                var property = new Property
                {
                    // Maximum length of standard nonclustered index column is 1700 bytes 850 * 2 = 1700
                    Name = new string('*', 851)
                };
                session.Save(property);
            }
            finally
            {
                if (session != null)
                {
                    Assert.Throws<SqlException>(() => session.Dispose());
                }
            }
        }

        [Fact]
        public async Task ThrowsWhenIndexKeysWithBitsLengthExceeded()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder
                        .DropMapIndexTable<PropertyIndex>();

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                        );

                    builder.AlterTable(nameof(PropertyIndex), table => table
                            .CreateIndex("IDX_Property", "Name", "ForRent", "IsOccupied"));

                    transaction.Commit();
                }
            }

            _store.RegisterIndexes<PropertyIndexProvider>();

            ISession session = null;
            try
            {
                session = _store.CreateSession();
                var property = new Property
                {
                    // Maximum length of standard nonclustered index column is 1700 bytes 850 * 2 = 1700
                    Name = new string('*', 850),
                    ForRent = true,
                    IsOccupied = true
                };
                session.Save(property);
            }
            finally
            {
                if (session != null)
                {
                    Assert.Throws<SqlException>(() => session.Dispose());
                }
            }
        }

        [Fact]
        public async Task ThrowsWhenIndexKeysLengthExceeded()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder
                        .DropMapIndexTable<PropertyIndex>();

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                        );

                    builder
                        .AlterTable(nameof(PropertyIndex), table => table
                            .CreateIndex("IDX_Property", "Name", "Location"));

                    transaction.Commit();
                }
            }

            _store.RegisterIndexes<PropertyIndexProvider>();

            ISession session = null;
            try
            {
                session = _store.CreateSession();
                var property = new Property
                {
                    // Maximum length of standard nonclustered index column is 1700 bytes 850 / 2 = 425
                    Name = new string('*', 425),
                    Location = new string('*', 426), // Max length + 2 bytes
                };
                session.Save(property);
            }
            finally
            {
                if (session != null)
                {
                    Assert.Throws<SqlException>(() => session.Dispose());
                }
            }
        }

        [Fact]
        public async Task ShouldIndexPropertyKey()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder
                        .DropMapIndexTable<PropertyIndex>();

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                        );

                    builder
                        .AlterTable(nameof(PropertyIndex), table => table
                            .CreateIndex("IDX_Property", "Name"));

                    transaction.Commit();
                }
            }

            _store.RegisterIndexes<PropertyIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var property = new Property
                {
                    // Maximum length of standard nonclustered index is 1700 bytes 850 * 2 = 1700
                    Name = new string('*', 850)
                };

                session.Save(property);
            }
        }


        [Fact]
        public async Task ShouldIndexPropertyKeys()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder
                        .DropMapIndexTable<PropertyIndex>();

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                        );

                    builder
                        .AlterTable(nameof(PropertyIndex), table => table
                            .CreateIndex("IDX_Property", "Name","Location"));

                    transaction.Commit();
                }
            }

            _store.RegisterIndexes<PropertyIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var property = new Property
                {
                    // Maximum length of standard nonclustered index is 1700 bytes 850 / 2 = 425
                    Name = new string('*', 425),
                    Location = new string('*', 425)
                };

                session.Save(property);
            }
        }

        [Fact]
        public async Task ShouldIndexPropertyKeysWithBits()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder
                        .DropMapIndexTable<PropertyIndex>();

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                        );

                    builder
                        .AlterTable(nameof(PropertyIndex), table => table
                            .CreateIndex("IDX_Property", "Name", "ForRent", "IsOccupied", "Location"));

                    transaction.Commit();
                }
            }

            _store.RegisterIndexes<PropertyIndexProvider>();

            using (var session = _store.CreateSession())
            {
                var property = new Property
                {
                    // Maximum length of standard nonclustered index is 1700 bytes 850 * 2 = 1700
                    Name = new string('*', 425),
                    IsOccupied = true,
                    ForRent = true,
                    Location = new string('*', 424)
                };

                session.Save(property);
            }
        }
        
        [Fact]
        public virtual async Task ShouldNotCauseInterlockASessionReadOnly()
        {
            IStore storeOfAnotherThread = await StoreFactory.CreateAndInitializeAsync(CreateConfiguration());
            storeOfAnotherThread.InitializeCollectionAsync("Collection1").GetAwaiter().GetResult();
            storeOfAnotherThread.TypeNames[typeof(Person)] = "People";
            // Create initial document
            using (var session = _store.CreateSession())
            {
                var email = new Person { Firstname = "Bill" };

                session.Save(email);
            }

            var viewModel = new Person
            {
                Firstname = "",
                Version = 0
            };

            using (var sessionFirstThread = _store.CreateSession())
            {
                var person = await sessionFirstThread.Query<Person>().FirstOrDefaultAsync();
                Assert.Equal("Bill", (string)person.Firstname);
                person.Firstname = "Billy";
                sessionFirstThread.Save(person);
                await sessionFirstThread.FlushAsync();

                try
                {
                    // this is the query another thread could be doing
                    using (var sessionSecondThread = storeOfAnotherThread.CreateSession())
                    {
                        var personSecondThread = await sessionSecondThread.Query<Person>().FirstOrDefaultAsync();
                        personSecondThread.Firstname = "George";
                        sessionSecondThread.Save(personSecondThread);
                        await sessionSecondThread.CommitAsync();
                    }
                }
                catch (Exception e)
                {
                    Assert.Contains("timeout", e.Message.ToLower());
                }
            }
            using (var sessionReadOnlyFirstThread = _store.CreateSessionReadOnly())
            {
                var person = await sessionReadOnlyFirstThread.Query<Person>().FirstOrDefaultAsync();
                Assert.Equal("Billy", (string)person.Firstname);

                using (var sessionSecondThread = storeOfAnotherThread.CreateSession())
                {
                    var personSecondThread = await sessionSecondThread.Query<Person>().FirstOrDefaultAsync();
                    personSecondThread.Firstname = "George";
                    sessionSecondThread.Save(personSecondThread);
                    await sessionSecondThread.FlushAsync();
                    await sessionSecondThread.CommitAsync();
                }
                using (var sessionFirstThread = _store.CreateSession())
                {
                    person.Firstname = "Bill";
                    sessionFirstThread.Import(person);
                    await sessionFirstThread.CommitAsync();
                }
                person = await sessionReadOnlyFirstThread.Query<Person>().FirstOrDefaultAsync();
                Assert.Equal("Bill", (string)person.Firstname);
            }

            storeOfAnotherThread.Dispose();
        }
    }
}
