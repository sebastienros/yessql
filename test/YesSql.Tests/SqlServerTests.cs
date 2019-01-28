using System;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.SqlServer;
using YesSql.Sql;

namespace YesSql.Tests
{
    public class SqlServerTests : CoreTests
    {
        public static string ConnectionString => Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING") ?? @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True";

        public SqlServerTests()
        {
        }

        protected override IStore CreateStore(Configuration configuration)
        {
            return new Store(new Configuration().UseSqlServer(ConnectionString).SetTablePrefix(TablePrefix).UseBlockIdGenerator());
        }

        protected override void OnCleanDatabase(SchemaBuilder builder, DbTransaction transaction)
        {
            base.OnCleanDatabase(builder, transaction);

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
