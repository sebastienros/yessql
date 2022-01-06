using Dapper;
using Microsoft.Data.SqlClient;
using System;
using Xunit.Abstractions;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class SqlServer2019Tests : SqlServerTests
    {

        public override SqlConnectionStringBuilder ConnectionStringBuilder => new(Environment.GetEnvironmentVariable("SQLSERVER_2019_CONNECTION_STRING") ?? @"Server=localhost;Database=Test;User Id=sa;Password=nvuBnK1e03yYgNPe9mOt");

        public SqlServer2019Tests(ITestOutputHelper output) : base(output)
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

        protected override void CreateDatabaseSchema(IConfiguration configuration)
        {
            if (!String.IsNullOrWhiteSpace(_configuration.SqlDialect.Schema))
            {
                using var connection = configuration.ConnectionFactory.CreateConnection();
                connection.Open();

                try
                {
                    connection.Execute($"CREATE SCHEMA { configuration.SqlDialect.Schema } AUTHORIZATION { configuration.SqlDialect.DefaultSchema };");
                }
                catch { }
            }
        }
    }
}
