using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using YesSql.Services;
using YesSql.Provider.InMemory;
using YesSql.Storage.InMemory;

namespace YesSql.Tests
{
    public class ProviderTests
    {
        [Fact]
        public async void AddedDbProviderStoreShouldPresentInDIContainer()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    // Act
                    services.AddDbProvider(config => config.UseInMemory());
                })
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        var store = context.RequestServices.GetService<IStore>();

                        // Assert
                        Assert.NotNull(store);
                        Assert.IsType<InMemoryDocumentStorageFactory>(store.Configuration.DocumentStorageFactory);
                        return Task.FromResult(0);
                    });
                });

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var response = await client.GetAsync("/");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
