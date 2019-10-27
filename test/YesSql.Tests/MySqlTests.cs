using System;
using System.Data.Common;
using System.Threading.Tasks;
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
            _store = await StoreFactory.CreateAsync(configuration);

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
            _store = await StoreFactory.CreateAsync(configuration);
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
    }
}
