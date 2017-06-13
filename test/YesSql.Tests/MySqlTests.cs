using System;
using YesSql.Provider.MySql;
using YesSql.Sql;

namespace YesSql.Tests
{
    public class MySqlTests : CoreTests
    {
        public static string ConnectionString => Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING") ?? @"server=localhost;uid=root;pwd=Password12!;database=yessql;Dialect=MySqlDialect";
        public MySqlTests()
        {
            _store = new Store(new Configuration().UseMySql(ConnectionString));

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
