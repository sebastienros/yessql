using System;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace YesSql.Tests.Fixtures
{
    /// <summary>
    /// Base fixture that manages SQL Server container lifecycle for tests.
    /// If SQLSERVER_CONNECTION_STRING environment variable is set, uses that instead of creating a container.
    /// </summary>
    public abstract class SqlServerContainerFixture : IAsyncLifetime
    {
        private const string EnvironmentVariableName = "SQLSERVER_CONNECTION_STRING";
        private MsSqlContainer _container;
        private readonly string _environmentConnectionString;

        protected abstract string DockerImage { get; }

        public string ConnectionString => _environmentConnectionString
            ?? _container?.GetConnectionString()
            ?? @"Server=127.0.0.1;Database=tempdb;User Id=sa;Password=Password12!;Encrypt=False";

        protected SqlServerContainerFixture()
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

            _container = new MsSqlBuilder()
                .WithImage(DockerImage)
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
