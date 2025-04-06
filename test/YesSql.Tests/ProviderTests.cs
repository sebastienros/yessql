using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.Sqlite;

namespace YesSql.Tests
{
    public sealed class ProviderTests : IDisposable
    {
        private readonly TemporaryFolder _tempFolder;

        public ProviderTests()
        {
            _tempFolder = new TemporaryFolder();
        }

        public void Dispose()
        {
            // _tempFolder.Dispose();
        }

        [Fact]
        public async Task AddedDbProviderStoreShouldPresentInDIContainer()
        {
            var connectionString = @"Data Source=" + _tempFolder.Folder + "yessql.db;Cache=Shared";

            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    // Act
                    services.AddDbProvider(config => config.UseSqLite(connectionString).UseDefaultIdGenerator());
                })
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        var store = context.RequestServices.GetService<IStore>();

                        // Assert
                        Assert.NotNull(store);
                        return Task.CompletedTask;
                    });
                });

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
