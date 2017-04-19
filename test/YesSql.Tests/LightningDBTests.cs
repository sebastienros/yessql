using System.Data;
using System.Data.SqlClient;
using YesSql.Provider.SqlServer;
using YesSql.Storage.LightningDB;

namespace YesSql.Tests
{
    public class LightningDBTests : CoreTests
    {
        private TemporaryFolder _tempFolder;
        public static string ConnectionString => @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True";

        public LightningDBTests()
        {
            _tempFolder = new TemporaryFolder();

            var configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<SqlConnection>(ConnectionString),
                IsolationLevel = IsolationLevel.ReadUncommitted,
                DocumentStorageFactory = new LightningDocumentStorageFactory(_tempFolder.Folder)
            };

            configuration.RegisterSqlServer();

            _store = new Store(configuration);

            CleanDatabase();
            CreateTables();
        }

        protected override void OnCleanDatabase(ISession session)
        {
            base.OnCleanDatabase(session);
        }
    }
}
