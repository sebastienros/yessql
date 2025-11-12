using Npgsql;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using YesSql.Provider.PostgreSql;
using YesSql.Sql;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    // Docker command
    // docker run --name postgresql -e POSTGRES_USER=root -e POSTGRES_PASSWORD=Password12! -e POSTGRES_DB=yessql -d -p 5432:5432 postgres:11
    public class PostgreSqlTests : CoreTests
    {
        public static NpgsqlConnectionStringBuilder ConnectionStringBuilder => new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING") ?? @"Server=localhost;Port=5432;Database=yessql;User Id=root;Password=Password12!;");

        protected override string DecimalColumnDefinitionFormatString => "decimal({0}, {1})";

        public PostgreSqlTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UsePostgreSql(ConnectionStringBuilder.ConnectionString, "BabyYoda")
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

        [Fact]
        public async Task ShouldQuoteForeignKeyConstraintsWithNumericPrefix()
        {
            // Test that foreign key constraints are properly quoted when table prefix starts with a digit
            // This addresses: https://github.com/sebastienros/yessql/issues/XXX
            var numericPrefixConfig = new Configuration()
                .UsePostgreSql(ConnectionStringBuilder.ConnectionString, "BabyYoda")
                .SetTablePrefix("1abcd_") // Prefix starting with a digit
                .UseBlockIdGenerator()
                .SetIdentityColumnSize(IdentityColumnSize.Int64);

            using var store = await StoreFactory.CreateAndInitializeAsync(numericPrefixConfig);

            await using var connection = store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using (var transaction = await connection.BeginTransactionAsync(store.Configuration.IsolationLevel))
            {
                var builder = new SchemaBuilder(store.Configuration, transaction);

                // This should not throw even with a numeric prefix
                await builder.CreateMapIndexTableAsync<PersonByName>(column => column
                    .Column<string>(nameof(PersonByName.SomeName))
                );

                await transaction.CommitAsync();
            }

            // Clean up
            await using (var transaction = await connection.BeginTransactionAsync(store.Configuration.IsolationLevel))
            {
                var builder = new SchemaBuilder(store.Configuration, transaction);
                await builder.DropMapIndexTableAsync<PersonByName>();
                await transaction.CommitAsync();
            }
        }
    }
}
