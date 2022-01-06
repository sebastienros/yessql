using Dapper;
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
                .UseSqlServer(ConnectionString, "Fett")
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                ;
        }

        protected override void CreateDatabaseSchema(IConfiguration configuration)
        {
            using var connection = configuration.ConnectionFactory.CreateConnection();
            connection.Open();

            try
            {
                // See https://docs.microsoft.com/en-us/sql/t-sql/statements/create-schema-transact-sql?view=sql-server-ver15
                connection.Execute($"CREATE SCHEMA { configuration.SqlDialect.Schema } AUTHORIZATION dbo;");
            }
            catch { }
        }
    }
}
