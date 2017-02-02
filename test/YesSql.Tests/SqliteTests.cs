using System.Data;
using Microsoft.Data.Sqlite;
using YesSql.Core.Services;
using YesSql.Storage.Sql;

namespace YesSql.Tests
{
    /// <summary>
    /// Run all tests with a SqlServer document storage
    /// </summary>
    public abstract class SqliteTests : CoreTests
    {
        private TemporaryFolder _tempFolder;

        public SqliteTests()
        {
            _tempFolder = new TemporaryFolder();

            var configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<SqliteConnection>(@"Data Source=" + _tempFolder.Folder + "yessql.db;Cache=Shared", true),
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

            session.ExecuteMigration(schemaBuilder => schemaBuilder
                .DropTable("Content"), false
            );

            session.ExecuteMigration(schemaBuilder => schemaBuilder
                .DropTable("Collection1_Content"), false
            );
        }
    }
}
