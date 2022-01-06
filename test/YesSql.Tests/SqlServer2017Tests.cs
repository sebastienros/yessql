using Dapper;
using Microsoft.Data.SqlClient;
using System;
using Xunit.Abstractions;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class SqlServer2017Tests : SqlServerTests
    {
        public override string ConnectionString
            => Environment.GetEnvironmentVariable("SQLSERVER_2017_CONNECTION_STRING")
                ?? @"Data Source=.;Initial Catalog=tempdb;Integrated Security=True"
                ;

        public SqlServer2017Tests(ITestOutputHelper output) : base(output)
        {
        }

        protected override IConfiguration CreateConfiguration()
        {
            return new Configuration()
                .UseSqlServer(ConnectionString, "BabyYoda")
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
                var builder = new SqlConnectionStringBuilder(ConnectionString);
                connection.Execute($"CREATE SCHEMA { configuration.SqlDialect.Schema } AUTHORIZATION { builder.UserID };");
            }
            catch { }
        }
    }
}
