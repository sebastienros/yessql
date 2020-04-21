using System;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;
using YesSql.Naming;
using YesSql.Provider.PostgreSql;
using YesSql.Sql;

namespace YesSql.Tests
{
    public class PostgreSqlTests : CoreTests
    {
        protected override NamingCase NamingCase => NamingCase.SnakeCase;
        public static string ConnectionString => Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING") ?? @"Server=localhost;Port=5432;Database=yessql;User Id=root;Password=Password12!;";

        protected override IConfiguration CreateConfiguration()
        {
            var configuration = new Configuration {NamingCase = NamingCase};
            return configuration
                .UsePostgreSql(ConnectionString)
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator();
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

        [Fact(Skip = "Stopped working on the CI for an unknown reason")]
        public override Task ShouldIndexWithDateTime()
        {
            return base.ShouldIndexWithDateTime();
        }

        [Fact(Skip = "Postgres locks on the table")]
        public override Task ShouldReadUncommittedRecords()
        {
            return base.ShouldReadUncommittedRecords();
        }
    }
}
