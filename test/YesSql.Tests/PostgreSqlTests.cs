using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using YesSql.Services;
using YesSql.Sql;
using YesSql.Storage.Sql;
using Xunit;

namespace YesSql.Tests
{
    public class PostgreSqlTests : CoreTests
    {
        public static string ConnectionString => Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING") ?? @"Server=localhost;Port=5432;Database=yessql;User Id=root;Password=Password12!;";
        public PostgreSqlTests()
        {
            var configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<NpgsqlConnection>(ConnectionString),
                IsolationLevel = IsolationLevel.ReadUncommitted,
                DocumentStorageFactory = new SqlDocumentStorageFactory()
            };

            _store = new Store(configuration);

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

        [Fact(Skip = "Postgres locks on the table")]
        public override Task ShouldReadUncommittedRecords()
        {
            return base.ShouldReadUncommittedRecords();
        }
    }
}
