using System;
using Xunit.Abstractions;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class SqlServer2008Tests : SqlServerTests
    {
        public SqlServer2008Tests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            var connectionString =
            Environment.GetEnvironmentVariable("SQLSERVER_2008_CONNECTION_STRING")
            ?? @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True"
            ;

            return new Configuration()
                .UseSqlServer2008(connectionString)
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                ;
        }
    }
}
