using System;
using System.Data.Common;
using System.Threading.Tasks;
using MySqlConnector;
using Xunit;
using YesSql.Provider.MySql;
using YesSql.Sql;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests
{
    /// <summary>
    /// To run MySQL inside Docker, use this command:
    /// docker run --name mysql -p 3306:3306 -d --env MYSQL_ROOT_PASSWORD=Password12! --env MYSQL_USER=user1 --env MYSQL_PASSWORD=Password12! --env MYSQL_DATABASE=yessql mysql:5.7 --max-allowed-packet=64000000
    /// </summary>
    public class MySqlTests : CoreTests
    {
        public static string ConnectionString => Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING") ?? @"server=localhost;uid=user1;pwd=Password12!;database=yessql;";
        public MySqlTests()
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseMySql(ConnectionString)
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

            using (var session = _store.CreateSession())
            {
                session.Save(bill);
            }

            // second store, don't register the index
            _store = await StoreFactory.CreateAndInitializeAsync(configuration);
            using (var session = _store.CreateSession())
            {
                var person = await session.Query().For<Person>().FirstOrDefaultAsync();
                Assert.NotNull(person);

                session.Delete(person);
            }
        }

        [Fact(Skip = "The syntax used for MySQL only works since MySQL 8.0 which is not available on appveyor")]
        public override void ShouldRenameColumn()
        {
            base.ShouldRenameColumn();
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
                        .DropMapIndexTable(nameof(PropertyIndex));

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(769))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(768))
                        );

                    Assert.Throws<MySqlException>(() => builder
                        .AlterTable(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name")));

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
                        .DropMapIndexTable(nameof(PropertyIndex));

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(384))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(384))
                        );

                    Assert.Throws<MySqlException>(() => builder
                        .AlterTable(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name", "ForRent", "IsOccupied", "Location")));

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
                        .DropMapIndexTable(nameof(PropertyIndex));

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(385))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(384))
                        );

                    Assert.Throws<MySqlException>(() => builder
                        .AlterTable(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name", "Location")));

                }
            }
        }

        [Fact]
        public async Task ShouldCreatePropertyIndexWithMaxKey()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder
                        .DropMapIndexTable(nameof(PropertyIndex));

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(768))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(768))
                        );

                    builder
                        .AlterTable(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name"));

                    transaction.Commit();
                }
            }
        }

        [Fact]
        public async Task ShouldCreateIndexPropertyWithMaxKeys()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder
                        .DropMapIndexTable(nameof(PropertyIndex));

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(384))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(384))
                        );

                    builder
                        .AlterTable(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name", "Location"));

                    transaction.Commit();
                }
            }
        }


        [Fact]
        public async Task ShouldCreateIndexPropertyWithMaxBitKeys()
        {
            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(_store.Configuration.IsolationLevel))
                {
                    var builder = new SchemaBuilder(_store.Configuration, transaction);

                    builder
                        .DropMapIndexTable(nameof(PropertyIndex));

                    builder
                        .CreateMapIndexTable<PropertyIndex>(column => column
                            .Column<string>(nameof(PropertyIndex.Name), col => col.WithLength(767))
                            .Column<bool>(nameof(PropertyIndex.ForRent))
                            .Column<bool>(nameof(PropertyIndex.IsOccupied))
                            .Column<string>(nameof(PropertyIndex.Location), col => col.WithLength(384))
                        );

                    builder
                        .AlterTable(nameof(PropertyIndex), table => table
                        .CreateIndex("IDX_Property", "Name", "ForRent", "IsOccupied"));

                    transaction.Commit();
                }
            }
        }
    }
}
