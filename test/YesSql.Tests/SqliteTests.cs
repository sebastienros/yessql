using Microsoft.Data.Sqlite;
using System;
using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.Sqlite;
using YesSql.Sql;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    /// <summary>
    /// Run all tests with a Sqlite document storage
    /// </summary>
    public class SqliteTests : CoreTests
    {
        private TemporaryFolder _tempFolder;

        protected override string DecimalColumnDefinitionFormatString => "NUMERIC";

        public SqliteTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            _tempFolder = new TemporaryFolder();
            var connectionString = @"Data Source=" + _tempFolder.Folder + "yessql.db;Cache=Shared";

            return new Configuration()
                .UseSqLite(connectionString)
                .SetTablePrefix(TablePrefix)
                .UseDefaultIdGenerator()
                .UseThreadSafetyChecks()
                .SetIdentityColumnSize(IdentityColumnSize.Int64);
        }

        [Fact(Skip = "Alter column is not supported by Sqlite")]
        public override void ShouldAlterColumn()
        {
            base.ShouldAlterColumn();
        }

        [Fact(Skip = "ReadCommitted is not supported by Sqlite")]
        public override Task ShouldReadCommittedRecords()
        {
            return base.ShouldReadCommittedRecords();
        }

        //        [Theory(Skip = "Sqlite doesn't use DbBlockIdGenerator")]
        //        [InlineData(100)]
        //#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
        //        public override Task ShouldGenerateLongIds(long id)
        //#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
        //        {
        //            return Task.CompletedTask;
        //        }

        [Fact(Skip = "Sqlite doesn't support concurrent writers")]
        public override Task ShouldReadUncommittedRecords()
        {
            return base.ShouldReadUncommittedRecords();
        }

        [Fact]
        public async Task ShouldSeedExistingIds()
        {
            using (_tempFolder = new TemporaryFolder())
            {
                var connectionString = @"Data Source=" + _tempFolder.Folder + "yessql.db;Cache=Shared";

                var store1 = await StoreFactory.CreateAndInitializeAsync(new Configuration().UseSqLite(connectionString).SetTablePrefix(TablePrefix).UseDefaultIdGenerator());

                await using (var session1 = store1.CreateSession())
                {
                    var p1 = new Person { Firstname = "Bill" };

                    await session1.SaveAsync(p1);

                    Assert.Equal(1, p1.Id);

                    await session1.SaveChangesAsync();
                }

                var store2 = await StoreFactory.CreateAndInitializeAsync(new Configuration().UseSqLite(connectionString).SetTablePrefix(TablePrefix).UseDefaultIdGenerator());

                await using var session2 = store2.CreateSession();
                var p2 = new Person { Firstname = "Bill" };

                await session2.SaveAsync(p2);

                Assert.Equal(2, p2.Id);
            }
        }

        [Theory(Skip = "Locking prevents Sqlite from concurrency issues")]
        [InlineData(true)]
        [InlineData(false)]
        public override Task ShouldHandleConcurrency(bool checkThreadSafety)
        {
            return base.ShouldHandleConcurrency(checkThreadSafety);
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
                        .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(4000))
                        .Column<bool>(nameof(PropertyIndex.ForRent))
                        .Column<bool>(nameof(PropertyIndex.IsOccupied))
                        .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(4000))
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
                Name = new string('*', 4000),
                IsOccupied = true,
                ForRent = true,
                Location = new string('*', 4000)
            };

            await session.SaveAsync(property);
        }
    }
}
