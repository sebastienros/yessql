using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using YesSql.Provider.MySql;
using YesSql.Provider.PostgreSql;
using YesSql.Provider.Sqlite;
using YesSql.Provider.SqlServer;

namespace YesSql.Tests
{
    public class ProviderTests : IDisposable
    {
        private TemporaryFolder _tempFolder;

        public ProviderTests()
        {
            _tempFolder = new TemporaryFolder();
        }

        public void Dispose()
        {
            _tempFolder.Dispose();
        }

        [Fact]
        public async void AddedDbProviderStoreShouldPresentInDIContainer()
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

        [Fact]
        public void CanRegisterProviderSeveralTimes()
        {
            for (var i = 0; i < 2; i++)
            {
                new Configuration()
                    .RegisterMySql()
                    .RegisterSqLite()
                    .RegisterPostgreSql()
                    .RegisterSqlServer();
            }
        }
    }
}
