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
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddDbProvider(config => config.UseSqLite(connectionString).UseDefaultIdGenerator());
        
            using var app = builder.Build();

            app.MapGet("/", context =>
            {
                var store = context.RequestServices.GetService<IStore>();

                // Assert
                Assert.NotNull(store);
                return Task.CompletedTask;
            });

            await app.StartAsync();
            var client = app.GetTestClient();
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
