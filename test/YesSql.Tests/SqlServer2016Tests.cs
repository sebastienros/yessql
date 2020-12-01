using System;
using Xunit.Abstractions;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class SqlServer2016Tests : SqlServerTests
    {
        public SqlServer2016Tests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            var connectionString = 
                Environment.GetEnvironmentVariable("SQLSERVER_2016_CONNECTION_STRING") 
                ?? @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True"
                ;

            return new Configuration()
                .UseSqlServer(connectionString)
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                ;
        }
    }
}
