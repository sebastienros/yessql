using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using YesSql.Core.Provider;
using YesSql.Core.Services;
using YesSql.Provider.InMemory;
using YesSql.Provider.MySql;
using YesSql.Provider.PostgreSql;
using YesSql.Provider.Sqlite;
using YesSql.Provider.SqlServer;
using YesSql.Storage.InMemory;
using YesSql.Storage.Sql;

namespace YesSql.Tests
{
    public class ProviderTests
    {
        [Fact]
        public void GetInMemoryProviderInfo()
        {
            // Arrange
            var options = new DbProviderOptions();

            // Act
            options.UseInMemory();

            // Assert
            Assert.Equal("InMemory", options.ProviderName);
            Assert.IsType<InMemoryDocumentStorageFactory>(options.Configuration.DocumentStorageFactory);
        }

        [Fact]
        public void GetSqliteProviderInfo()
        {
            // Arrange
            var options = new DbProviderOptions();

            // Act
            options.UseSqLite("ConnectionString");

            // Assert
            Assert.Equal("SQLite", options.ProviderName);
            Assert.IsType<SqlDocumentStorageFactory>(options.Configuration.DocumentStorageFactory);
        }

        [Fact]
        public void GetSqlServerProviderInfo()
        {
            // Arrange
            var options = new DbProviderOptions();

            // Act
            options.UseSqlServer("ConnectionString");

            // Assert
            Assert.Equal("SQL Server", options.ProviderName);
            Assert.IsType<SqlDocumentStorageFactory>(options.Configuration.DocumentStorageFactory);
        }

        [Fact]
        public void GetMySqlProviderInfo()
        {
            // Arrange
            var options = new DbProviderOptions();

            // Act
            options.UseMySql("ConnectionString");

            // Assert
            Assert.Equal("MySQL", options.ProviderName);
            Assert.IsType<SqlDocumentStorageFactory>(options.Configuration.DocumentStorageFactory);
        }

        [Fact]
        public void GetPostgreSqlProviderInfo()
        {
            // Arrange
            var options = new DbProviderOptions();

            // Act
            options.UsePostgreSql("ConnectionString");

            // Assert
            Assert.Equal("PostgreSQL", options.ProviderName);
            Assert.IsType<SqlDocumentStorageFactory>(options.Configuration.DocumentStorageFactory);
        }

        [Fact]
        public async void AddedDbProviderStoreShouldPresentInDIContainer()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    // Act
                    services.AddDbProvider(options => options.UseInMemory());
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
