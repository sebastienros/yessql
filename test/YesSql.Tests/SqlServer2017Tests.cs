using Dapper;
using Microsoft.Data.SqlClient;
using System;
using Xunit.Abstractions;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class SqlServer2017Tests : SqlServerTests
    {
        public override SqlConnectionStringBuilder ConnectionStringBuilder => new(Environment.GetEnvironmentVariable("SQLSERVER_2017_CONNECTION_STRING") ?? @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True");

        public SqlServer2017Tests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseSqlServer(ConnectionStringBuilder.ConnectionString, "BobaFett")
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                ;
        }
    }
}
