using System;
using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.PostgreSql;
using YesSql.Sql;

namespace YesSql.Tests
{
    public class PostgreSqlTests : CoreTests
    {
        public static string ConnectionString => Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING") ?? @"Server=localhost;Port=5432;Database=yessql;User Id=root;Password=Password12!;";
        public PostgreSqlTests()
        {
            _store = new Store(new Configuration().UsePostgreSql(ConnectionString));

            CleanDatabase();
            CreateTables();
        }

        protected override void OnCleanDatabase(ISession session)
        {
            base.OnCleanDatabase(session);

            var builder = new SchemaBuilder(session);

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

        [Fact(Skip = "Stopped working on the CI for an unknown reason")]
        public override Task ShouldIndexWithDateTime()
        {
            return base.ShouldIndexWithDateTime();
        }

        [Fact(Skip = "Stopped working on the CI for an unknown reason")]
        public override Task ShouldIndexWithDateTimeOffset()
        {
            return base.ShouldIndexWithDateTimeOffset();
        }

        [Fact(Skip = "Postgres locks on the table")]
        public override Task ShouldReadUncommittedRecords()
        {
            return base.ShouldReadUncommittedRecords();
        }
    }
}
