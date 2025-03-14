using Microsoft.Data.SqlClient;
using System;
using Xunit.Abstractions;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class SqlServer2019Tests : SqlServerTests
    {
        // Docker command
        // docker run --name sqlserver2019 -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Password12!" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest
        public override SqlConnectionStringBuilder ConnectionStringBuilder => new(Environment.GetEnvironmentVariable("SQLSERVER_2019_CONNECTION_STRING") ?? @"Server=127.0.0.1;Database=tempdb;User Id=sa;Password=Password12!;Encrypt=False");

        public SqlServer2019Tests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseSqlServer(ConnectionStringBuilder.ConnectionString, "BobaFett")
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                .UseThreadSafetyChecks()
                .SetIdentityColumnSize(IdentityColumnSize.Int64);
        }
    }
}
