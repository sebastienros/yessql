using Microsoft.Data.SqlClient;
using System;
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
        public abstract SqlConnectionStringBuilder ConnectionStringBuilder { get; }

        protected SqlServerTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseSqlServer(ConnectionStringBuilder.ConnectionString, "BobaFett")
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                .SetIdentityColumnSize(IdentityColumnSize.Int64)
                ;
        }

        [Fact]
        public async Task ShouldSeedExistingIds()
        {
            var configuration = new Configuration().UseSqlServer(ConnectionStringBuilder.ConnectionString, "BobaFett").SetTablePrefix("Store1").UseBlockIdGenerator();

            await using (var connection = configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync();
                var builder = new SchemaBuilder(configuration, transaction, throwOnError: false);

                await builder.DropTableAsync(configuration.TableNameConvention.GetDocumentTable(""));
                await builder.DropTableAsync("Identifiers");

                await transaction.CommitAsync();
            }

            var store1 = await StoreFactory.CreateAndInitializeAsync(configuration);

            await using (var session1 = store1.CreateSession())
            {
                var p1 = new Person { Firstname = "Bill" };

                await session1.SaveAsync(p1);

                Assert.Equal(1, p1.Id);

                await session1.SaveChangesAsync();
            }

            var store2 = await StoreFactory.CreateAndInitializeAsync(new Configuration().UseSqlServer(ConnectionStringBuilder.ConnectionString, "BobaFett").SetTablePrefix("Store1").UseBlockIdGenerator());

            await using var session2 = store2.CreateSession();
            var p2 = new Person { Firstname = "Bill" };

            await session2.SaveAsync(p2);

            Assert.Equal(21, p2.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Collection1")]
        public async Task ShouldGenerateIdsWithConcurrentStores(string collection)
        {
            var configuration = new Configuration().UseSqlServer(ConnectionStringBuilder.ConnectionString, "BobaFett").SetTablePrefix("Store1").UseBlockIdGenerator();

            await using (var connection = configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync(configuration.IsolationLevel);
                var builder = new SchemaBuilder(configuration, transaction, throwOnError: false);

                await builder.DropTableAsync(configuration.TableNameConvention.GetDocumentTable(""));
                await builder.DropTableAsync("Identifiers");

                await transaction.CommitAsync();
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
                    lastId = taskId = await store1.Configuration.IdGenerator.GetNextIdAsync(collection);

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
            await using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
                var builder = new SchemaBuilder(_store.Configuration, transaction);

                await builder
                    .DropMapIndexTableAsync<PropertyIndex>();

                await builder
                    .CreateMapIndexTableAsync<PropertyIndex>(column => column
                        .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                        .Column<bool>(nameof(PropertyIndex.ForRent))
                        .Column<bool>(nameof(PropertyIndex.IsOccupied))
                        .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                    );

                await builder
                    .AlterTableAsync(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name"));

                await transaction.CommitAsync();
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
                await session.SaveAsync(property);
            }
            finally
            {
                if (session != null)
                {
                    await Assert.ThrowsAsync<SqlException>(session.SaveChangesAsync);
                    await session.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task ThrowsWhenIndexKeysWithBitsLengthExceeded()
        {
            await using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
                var builder = new SchemaBuilder(_store.Configuration, transaction);

                await builder
                    .DropMapIndexTableAsync<PropertyIndex>();

                await builder
                    .CreateMapIndexTableAsync<PropertyIndex>(column => column
                        .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                        .Column<bool>(nameof(PropertyIndex.ForRent))
                        .Column<bool>(nameof(PropertyIndex.IsOccupied))
                        .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                    );

                await builder.AlterTableAsync(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name", "ForRent", "IsOccupied"));

                await transaction.CommitAsync();
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
                await session.SaveAsync(property);
            }
            finally
            {
                if (session != null)
                {
                    await Assert.ThrowsAsync<SqlException>(session.SaveChangesAsync);
                    await session.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task ThrowsWhenIndexKeysLengthExceeded()
        {
            await using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
                var builder = new SchemaBuilder(_store.Configuration, transaction);

                await builder
                    .DropMapIndexTableAsync<PropertyIndex>();

                await builder
                    .CreateMapIndexTableAsync<PropertyIndex>(column => column
                        .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                        .Column<bool>(nameof(PropertyIndex.ForRent))
                        .Column<bool>(nameof(PropertyIndex.IsOccupied))
                        .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                    );

                await builder
                    .AlterTableAsync(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name", "Location"));

                await transaction.CommitAsync();
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
                await session.SaveAsync(property);
            }
            finally
            {
                if (session != null)
                {
                    await Assert.ThrowsAsync<SqlException>(session.SaveChangesAsync);
                    await session.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task ShouldIndexPropertyKey()
        {
            await using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
                try
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    await builder
                        .DropMapIndexTableAsync<PropertyIndex>();

                    await builder
                        .CreateMapIndexTableAsync<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                        );

                    await builder
                        .AlterTableAsync(nameof(PropertyIndex), table => table
                            .CreateIndex("IDX_Property", "Name"));

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                }
            }

            _store.RegisterIndexes<PropertyIndexProvider>();

            await using var session = _store.CreateSession();
            var property = new Property
            {
                // Maximum length of standard nonclustered index is 1700 bytes 850 * 2 = 1700
                Name = new string('*', 850)
            };

            await session.SaveAsync(property);
        }


        [Fact]
        public async Task ShouldIndexPropertyKeys()
        {
            await using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
                var builder = new SchemaBuilder(_store.Configuration, transaction);

                await builder
                    .DropMapIndexTableAsync<PropertyIndex>();

                await builder
                    .CreateMapIndexTableAsync<PropertyIndex>(column => column
                        .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                        .Column<bool>(nameof(PropertyIndex.ForRent))
                        .Column<bool>(nameof(PropertyIndex.IsOccupied))
                        .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                    );

                await builder
                    .AlterTableAsync(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name", "Location"));

                await transaction.CommitAsync();
            }

            _store.RegisterIndexes<PropertyIndexProvider>();

            await using var session = _store.CreateSession();
            var property = new Property
            {
                // Maximum length of standard nonclustered index is 1700 bytes 850 / 2 = 425
                Name = new string('*', 425),
                Location = new string('*', 425)
            };

            await session.SaveAsync(property);
        }

        [Fact]
        public async Task ShouldIndexPropertyKeysWithBits()
        {
            await using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
                var builder = new SchemaBuilder(_store.Configuration, transaction);

                await builder
                    .DropMapIndexTableAsync<PropertyIndex>();

                await builder
                    .CreateMapIndexTableAsync<PropertyIndex>(column => column
                        .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(1000))
                        .Column<bool>(nameof(PropertyIndex.ForRent))
                        .Column<bool>(nameof(PropertyIndex.IsOccupied))
                        .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(1000))
                    );

                await builder
                    .AlterTableAsync(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name", "ForRent", "IsOccupied", "Location"));

                await transaction.CommitAsync();
            }

            _store.RegisterIndexes<PropertyIndexProvider>();

            await using var session = _store.CreateSession();
            var property = new Property
            {
                // Maximum length of standard nonclustered index is 1700 bytes 850 * 2 = 1700
                Name = new string('*', 425),
                IsOccupied = true,
                ForRent = true,
                Location = new string('*', 424)
            };

            await session.SaveAsync(property);
        }
    }
}
