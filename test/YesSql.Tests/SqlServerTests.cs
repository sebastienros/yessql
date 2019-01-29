using System;
using System.Data.Common;
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
                    var builder = new SchemaBuilder(configuration, transaction);

                    builder.DropTable("Document");
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

    }
}
