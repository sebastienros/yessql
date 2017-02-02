using System.Data;
using System.Data.SqlClient;
using YesSql.Core.Services;
using YesSql.Storage.InMemory;

namespace YesSql.Tests
{
    public abstract class InMemoryTests : CoreTests
    {
        public static string ConnectionString => @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True";

        public InMemoryTests()
        {
            var configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<SqlConnection>(ConnectionString),
                IsolationLevel = IsolationLevel.ReadUncommitted,
                DocumentStorageFactory = new InMemoryDocumentStorageFactory()
            };

            _store = new Store(configuration);

            CleanDatabase();
            CreateTables();
        }

        protected override void OnCleanDatabase(ISession session)
        {
            base.OnCleanDatabase(session);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
        }
    }
}
