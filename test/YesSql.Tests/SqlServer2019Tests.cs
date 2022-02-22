using System;
using Xunit.Abstractions;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class SqlServer2019Tests : SqlServerTests
    {

        // docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Password12!" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest

        public override string ConnectionString 
            =>  Environment.GetEnvironmentVariable("SQLSERVER_2019_CONNECTION_STRING") 
                ?? "Server=127.0.0.1;Database=tempdb;Integrated Security=False;User Id=sa;Password=Password12!;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True"
                // ?? @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True"
                ;

        public SqlServer2019Tests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseSqlServer(ConnectionString)
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                ;
        }
    }
}
