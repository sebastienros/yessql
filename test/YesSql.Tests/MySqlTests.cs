using System;
using YesSql.Provider.MySql;
using YesSql.Sql;

namespace YesSql.Tests
{
    public class MySqlTests : CoreTests
    {
        public static string ConnectionString => Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING") ?? @"server=localhost;uid=user1;pwd=Password12!;database=yessql;";
        public MySqlTests()
        {
            _store = new Store(new Configuration().UseMySql(ConnectionString).SetTablePrefix(TablePrefix));

            CleanDatabase(false);
            CreateTables();
        }

        protected override void OnCleanDatabase(SchemaBuilder builder, ISession session)
        {
            base.OnCleanDatabase(builder, session);

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
