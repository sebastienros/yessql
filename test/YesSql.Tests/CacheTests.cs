using System.Data;
using System.Data.SqlClient;
using YesSql.Core.Services;
using YesSql.Storage.Cache;
using YesSql.Storage.Sql;

namespace YesSql.Tests
{
    public class CacheTests : CoreTests
    {
        public static string ConnectionString => @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True";

        public CacheTests()
        {
            var configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<SqlConnection>(ConnectionString),
                IsolationLevel = IsolationLevel.ReadUncommitted,
                DocumentStorageFactory = new CacheDocumentStorageFactory(new SqlDocumentStorageFactory())
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
        }
    }
}
