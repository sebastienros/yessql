using System;
using YesSql.Provider.SqlServer;
using YesSql.Sql;

namespace YesSql.Tests
{
    public class SqlServerTests : CoreTests
    {
        public static string ConnectionString => Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING") ?? @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True";

        public SqlServerTests()
        {
            _store = new Store(new Configuration().UseSqlServer(ConnectionString));

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
