using System;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace YesSql.Tests.Fixtures
{
    /// <summary>
    /// Fixture that manages a PostgreSQL container shared across all tests in a test class.
    /// If POSTGRESQL_CONNECTION_STRING environment variable is set, uses that instead of creating a container.
    /// </summary>
    public class PostgreSqlContainerFixture : IAsyncLifetime
    {
        private const string EnvironmentVariableName = "POSTGRESQL_CONNECTION_STRING";
        private PostgreSqlContainer _container;
        private readonly string _environmentConnectionString;

        public string ConnectionString => _environmentConnectionString
            ?? _container?.GetConnectionString()
            ?? @"Server=localhost;Port=5432;Database=yessql;User Id=root;Password=Password12!;";

        public PostgreSqlContainerFixture()
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

            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("yessql")
                .WithUsername("root")
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
