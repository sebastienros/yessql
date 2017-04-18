using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using YesSql.Services;
using YesSql.Sql;
using YesSql.Storage.Sql;
using Xunit;

namespace YesSql.Tests
{
    /// <summary>
    /// Run all tests with a SqlServer document storage
    /// </summary>
    public class SqliteTests : CoreTests
    {
        private TemporaryFolder _tempFolder;

        public SqliteTests()
        {
            _tempFolder = new TemporaryFolder();

            var configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<SqliteConnection>(@"Data Source=" + _tempFolder.Folder + "yessql.db;Cache=Shared"),
                IsolationLevel = IsolationLevel.Serializable,
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

        [Fact(Skip = "ReadCommitted is not supported by Sqlite")]
        public override Task ShouldReadCommittedRecords()
        {
            return base.ShouldReadCommittedRecords();
        }

        [Fact(Skip = "Sqlite doesn't support concurrent writers")]
        public override Task ShouldReadUncommittedRecords()
        {
            return base.ShouldReadUncommittedRecords();
        }
    }
}
