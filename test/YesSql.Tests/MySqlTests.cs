using MySqlConnector;
using System;
using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.MySql;
using YesSql.Sql;
using YesSql.Tests.Fixtures;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    /// <summary>
    /// Run all tests with a MySQL document storage using Testcontainers.
    /// </summary>
    [Collection("MySql")]
    public class MySqlTests : CoreTests
    {
        private readonly MySqlContainerFixture _fixture;

        public string ConnectionString => _fixture.ConnectionString;

        protected override string DecimalColumnDefinitionFormatString => "decimal({0}, {1})";

        public MySqlTests(MySqlContainerFixture fixture, ITestOutputHelper output) : base(output)
        {
            _fixture = fixture;
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseMySql(ConnectionString)
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                .WithThreadSafetyChecks()
                .SetIdentityColumnSize(IdentityColumnSize.Int64);
        }

        [Fact]
        public async Task ForeignKeyOfIndexesMustBe_DeleteCascated()
        {
            var configuration = CreateConfiguration();
            _store = await StoreFactory.CreateAndInitializeAsync(configuration);

            // First store register the index
            _store.RegisterIndexes<PersonIndexProvider>();

            var bill = new Person
            {
                Firstname = "Bill",
                Lastname = "Gates"
            };

            await using (var session = _store.CreateSession())
            {
                await session.SaveAsync(bill);

                await session.SaveChangesAsync();
            }

            // second store, don't register the index
            _store = await StoreFactory.CreateAndInitializeAsync(configuration);
            await using (var session = _store.CreateSession())
            {
                var person = await session.Query().For<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                session.Delete(person);
            }
        }

        [Fact]
        public async Task ThrowsWhenIndexKeyLengthExceeded()
        {
            await using var connection = _store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
            var builder = new SchemaBuilder(_store.Configuration, transaction);

            await builder.DropMapIndexTableAsync<PropertyIndex>();

            await builder.CreateMapIndexTableAsync<PropertyIndex>(column => column
                .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(769))
                .Column<bool>(nameof(PropertyIndex.ForRent))
                .Column<bool>(nameof(PropertyIndex.IsOccupied))
                .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(768))
            );

            await Assert.ThrowsAsync<MySqlException>(async () =>
            {
                await builder.AlterTableAsync(nameof(PropertyIndex), table => table
                    .CreateIndex("IDX_Property", "Name")
                );
            });
        }

        [Fact]
        public async Task ShouldCreatePropertyIndexWithSpecifiedLength()
        {
            await using var connection = _store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
            var builder = new SchemaBuilder(_store.Configuration, transaction);

            await builder.DropMapIndexTableAsync<PropertyIndex>();

            await builder.CreateMapIndexTableAsync<PropertyIndex>(column => column
                .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(769))
                .Column<bool>(nameof(PropertyIndex.ForRent))
                .Column<bool>(nameof(PropertyIndex.IsOccupied))
                .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(768))
            );

            // 300 + 468 = 768 which is the max allowed by MySQL.
            await builder.AlterTableAsync(nameof(PropertyIndex), table => table
                .CreateIndex("IDX_Property", "Name(300)", "Location (468)")
            );

            await transaction.CommitAsync();
        }

        [Fact]
        public async Task ThrowsWhenIndexKeysWithBitsLengthExceeded()
        {
            await using var connection = _store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
            var builder = new SchemaBuilder(_store.Configuration, transaction);

            await builder.DropMapIndexTableAsync<PropertyIndex>();

            await builder.CreateMapIndexTableAsync<PropertyIndex>(column => column
                .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(384))
                .Column<bool>(nameof(PropertyIndex.ForRent))
                .Column<bool>(nameof(PropertyIndex.IsOccupied))
                .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(384))
            );

            await Assert.ThrowsAsync<MySqlException>(async () =>
            {
                await builder.AlterTableAsync(nameof(PropertyIndex), table => table
                    .CreateIndex("IDX_Property", "Name", "ForRent", "IsOccupied", "Location"));
            });
        }

        [Fact]
        public async Task ThrowsWhenIndexKeysLengthExceeded()
        {
            await using var connection = _store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
            var builder = new SchemaBuilder(_store.Configuration, transaction);

            await builder.DropMapIndexTableAsync<PropertyIndex>();

            await builder.CreateMapIndexTableAsync<PropertyIndex>(column => column
                .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(385))
                .Column<bool>(nameof(PropertyIndex.ForRent))
                .Column<bool>(nameof(PropertyIndex.IsOccupied))
                .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(384))
            );

            await Assert.ThrowsAsync<MySqlException>(async () => await builder
                .AlterTableAsync(nameof(PropertyIndex), table => table
                .CreateIndex("IDX_Property", "Name", "Location")));
        }

        [Fact]
        public async Task ShouldCreatePropertyIndexWithMaxKey()
        {
            await using var connection = _store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
            var builder = new SchemaBuilder(_store.Configuration, transaction);

            await builder.DropMapIndexTableAsync<PropertyIndex>();

            await builder.CreateMapIndexTableAsync<PropertyIndex>(column => column
                .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(768))
                .Column<bool>(nameof(PropertyIndex.ForRent))
                .Column<bool>(nameof(PropertyIndex.IsOccupied))
                .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(768))
            );

            await builder.AlterTableAsync(nameof(PropertyIndex), table => table
                .CreateIndex("IDX_Property", "Name"));

            await transaction.CommitAsync();
        }

        [Fact]
        public async Task ShouldCreateIndexPropertyWithMaxKeys()
        {
            await using var connection = _store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
            var builder = new SchemaBuilder(_store.Configuration, transaction);

            await builder.DropMapIndexTableAsync<PropertyIndex>();

            await builder
                .CreateMapIndexTableAsync<PropertyIndex>(column => column
                    .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(384))
                    .Column<bool>(nameof(PropertyIndex.ForRent))
                    .Column<bool>(nameof(PropertyIndex.IsOccupied))
                    .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(384))
                );

            await builder
                .AlterTableAsync(nameof(PropertyIndex), table => table
                .CreateIndex("IDX_Property", "Name", "Location"));

            await transaction.CommitAsync();
        }

        [Fact]
        public async Task ShouldCreateIndexPropertyWithMaxBitKeys()
        {
            await using var connection = _store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel);
            var builder = new SchemaBuilder(_store.Configuration, transaction);

            await builder
                .DropMapIndexTableAsync<PropertyIndex>();

            await builder
                .CreateMapIndexTableAsync<PropertyIndex>(column => column
                    .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(767))
                    .Column<bool>(nameof(PropertyIndex.ForRent))
                    .Column<bool>(nameof(PropertyIndex.IsOccupied))
                    .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(384))
                );

            await builder
                .AlterTableAsync(nameof(PropertyIndex), table => table
                .CreateIndex("IDX_Property", "Name", "ForRent", "IsOccupied"));

            await transaction.CommitAsync();
        }

        [Fact]
        public async Task ShouldCreateHashedIndexKeyName()
        {
            await _store.InitializeCollectionAsync("LongCollection");

            await using var connection = _store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using (var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel))
            {
                var builder = new SchemaBuilder(_store.Configuration, transaction);

                // tpFK_LongCollection_PersonsByNameCol_LongCollection_Document_Id : 64 chars. Throws exception if not hashed.

                await builder.CreateReduceIndexTableAsync<PersonsByNameCol>(column => column
                    .Column<string>(nameof(PersonsByNameCol.Name))
                    .Column<int>(nameof(PersonsByNameCol.Count)),
                    "LongCollection"
                    );

                await transaction.CommitAsync();
            }

            await using (var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel))
            {
                var builder = new SchemaBuilder(_store.Configuration, transaction);

                await builder.DropReduceIndexTableAsync<PersonsByNameCol>("LongCollection");
                await transaction.CommitAsync();
            }
        }
    }
}
