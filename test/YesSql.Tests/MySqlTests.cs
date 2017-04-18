using System.Data;
using YesSql.Services;
using YesSql.Sql;
using YesSql.Storage.Sql;
using MySql.Data.MySqlClient;
using System;

namespace YesSql.Tests
{
    public class MySqlTests : CoreTests
    {
        public static string ConnectionString => Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING") ?? @"server=localhost;uid=root;pwd=Password12!;database=yessql;";
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
    }
}
