using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;
using Xunit.Abstractions;
using YesSql.Provider.PostgreSql;
using YesSql.Sql;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    public class PostgreSqlTests : CoreTests
    {
        protected override string DecimalColumnDefinitionFormatString => "decimal({0}, {1})";
        
        protected readonly PostgreSqlContainer PostgreSqlContainer = new PostgreSqlBuilder().Build();

        protected PostgreSqlTests(ITestOutputHelper output) : base(output)
        {
        }

        public override async Task InitializeAsync()
        {
            await PostgreSqlContainer.StartAsync();
            await base.InitializeAsync();
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UsePostgreSql(PostgreSqlContainer.GetConnectionString(), "BabyYoda")
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                .SetIdentityColumnSize(IdentityColumnSize.Int64)
                ;
        }

        [Fact(Skip = "Postgres locks on the table")]
        public override Task ShouldReadUncommittedRecords()
        {
            return base.ShouldReadUncommittedRecords();
        }

        [Fact]
        public async Task ShouldIndexPropertyKeys()
        {
            await using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                await using (var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    await builder.DropMapIndexTableAsync<PropertyIndex>();

                    await transaction.CommitAsync();
                }

                await using (var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);
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

        [Fact]
        public async Task ShouldCreateHashedIndexKeyName()
        {
            // NB: Postgres will not throw here when the key is too long. It will simply truncate it.
            // This will cause exceptions in other tables when the 'short' key is truncated again.
            await _store.InitializeCollectionAsync("LongCollection");

            await using var connection = _store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using (var transaction = await connection.BeginTransactionAsync(_store.Configuration.IsolationLevel))
            {
                var builder = new SchemaBuilder(_store.Configuration, transaction);

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

        public async override Task DisposeAsync() => await PostgreSqlContainer.DisposeAsync();
    }
}
