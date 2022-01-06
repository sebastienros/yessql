using System;
using Xunit.Abstractions;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class SqlServer2019Tests : SqlServerTests
    {

        public override string ConnectionString 
            =>  Environment.GetEnvironmentVariable("SQLSERVER_2019_CONNECTION_STRING") 
                ?? @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True"
                ;

        public SqlServer2019Tests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseSqlServer(ConnectionString, "test")
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                ;
        }
    }
}
