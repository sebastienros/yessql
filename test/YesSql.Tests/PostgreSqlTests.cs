using System;
using System.Data.Common;
using System.Threading.Tasks;
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
        public static string ConnectionString => Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING") ?? @"Server=localhost;Port=5432;Database=yessql;User Id=root;Password=Password12!;";

        protected override string DecimalColumnDefinitionFormatString => "decimal({0}, {1})";

        public PostgreSqlTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UsePostgreSql(ConnectionString)
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

        [Fact(Skip = "Postgres locks on the table")]
        public override Task ShouldReadUncommittedRecords()
        {
            return base.ShouldReadUncommittedRecords();
        }
        [Fact]
        public async Task ShouldIndexPropertyKeys()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    new SchemaBuilder(_store.Configuration, transaction)
                        .DropMapIndexTable<PropertyIndex>();

                    transaction.Commit();
                }

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);
                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(4000))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(4000))
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
                    Name = new string('*', 4000),
                    IsOccupied = true,
                    ForRent = true,
                    Location = new string('*', 4000)
                };

                session.Save(property);
            }
        }

        [Fact]
        public async Task ShouldCreateHashedIndexKeyName()
        {
            // NB: Postgres will not throw here when the key is too long. It will simply truncate it.
            // This will cause exceptions in other tables when the 'short' key is truncated again.
            await _store.InitializeCollectionAsync("LongCollection");

            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.CreateReduceIndexTable<PersonsByNameCol>(column => column
                        .Column<string>(nameof(PersonsByNameCol.Name))
                        .Column<int>(nameof(PersonsByNameCol.Count)),
                        "LongCollection"
                        );

                    transaction.Commit();
                }

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder.DropReduceIndexTable<PersonsByNameCol>("LongCollection");
                    transaction.Commit();
                }
            }
        }        
    }
}
