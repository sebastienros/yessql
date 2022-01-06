using Dapper;
using Microsoft.Data.SqlClient;
using System;
using Xunit.Abstractions;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class SqlServer2019Tests : SqlServerTests
    {

        public override SqlConnectionStringBuilder ConnectionStringBuilder => new(Environment.GetEnvironmentVariable("SQLSERVER_2019_CONNECTION_STRING") ?? @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True");

        public SqlServer2019Tests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseSqlServer(ConnectionStringBuilder.ConnectionString, "Fett")
                .SetTablePrefix(TablePrefix)
                .UseBlockIdGenerator()
                ;
        }

        protected override void CreateDatabaseSchema(IConfiguration configuration)
        {
            if (ConnectionStringBuilder.UserID != configuration.SqlDialect.DefaultSchema)
            {
                using var connection = configuration.ConnectionFactory.CreateConnection();
                connection.Open();

                try
                {
                    connection.Execute($"CREATE SCHEMA { configuration.SqlDialect.Schema } AUTHORIZATION { ConnectionStringBuilder.UserID };");
                }
                catch { }
            }
        }
    }
}
