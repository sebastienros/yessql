using System.Data;
using YesSql.Core.Services;
using YesSql.Storage.Sql;
using MySql.Data.MySqlClient;

namespace YesSql.Tests
{
    public class MySqlTests : CoreTests
    {
        public static string ConnectionString => @"server=127.0.0.1;uid=root;pwd=Password12!;database=yessql;";
        public MySqlTests()
        {
            var configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<MySqlConnection>(ConnectionString),
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
    }
}
