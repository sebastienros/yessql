using System;
using System.Threading.Tasks;
using Testcontainers.MySql;
using Xunit;

namespace YesSql.Tests.Fixtures
{
    /// <summary>
    /// Fixture that manages a MySQL container shared across all tests in a test class.
    /// If MYSQL_CONNECTION_STRING environment variable is set, uses that instead of creating a container.
    /// </summary>
    public class MySqlContainerFixture : IAsyncLifetime
    {
        private const string EnvironmentVariableName = "MYSQL_CONNECTION_STRING";
        private MySqlContainer _container;
        private readonly string _environmentConnectionString;

        public string ConnectionString => _environmentConnectionString
            ?? _container?.GetConnectionString()
            ?? @"server=localhost;uid=user1;pwd=Password12!;database=yessql;";

        public MySqlContainerFixture()
        {
            _environmentConnectionString = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        }

        public async ValueTask InitializeAsync()
        {
            // Skip container creation if connection string is provided via environment variable
            if (!string.IsNullOrEmpty(_environmentConnectionString))
            {
                return;
            }

            _container = new MySqlBuilder()
                .WithImage("mysql:8")
                .WithDatabase("yessql")
                .WithUsername("user1")
                .WithPassword("Password12!")
                .Build();

            await _container.StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_container != null)
            {
                await _container.DisposeAsync();
            }

            GC.SuppressFinalize(this);
        }
    }
}
