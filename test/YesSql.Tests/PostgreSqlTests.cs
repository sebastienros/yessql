using System.Data;
using YesSql.Core.Services;
using YesSql.Storage.Sql;
using System;
using Npgsql;
using System.Threading.Tasks;
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

            session.ExecuteMigration(schemaBuilder => schemaBuilder
                .DropTable("Content"), false
            );

            session.ExecuteMigration(schemaBuilder => schemaBuilder
                .DropTable("Collection1_Content"), false
            );
        }

        [Fact(Skip = "Postgre locks on the table")]
        public override Task ShouldReadUncommittedRecords()
        {
            return base.ShouldReadUncommittedRecords();
        }
    }
}
