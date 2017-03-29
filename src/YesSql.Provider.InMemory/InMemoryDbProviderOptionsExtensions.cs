using Microsoft.Data.Sqlite;
using System;
using System.Data;
using YesSql.Core.Provider;
using YesSql.Core.Services;
using YesSql.Storage.InMemory;

namespace YesSql.Provider.InMemory
{
    public static class InMemoryDbProviderOptionsExtensions
    {
        private const string ConnectionString = "Data Source=:memory:";

        public static void UseInMemory(
            this IDbProviderOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<SqliteConnection>(ConnectionString, true),
                DocumentStorageFactory = new InMemoryDocumentStorageFactory(),
                IsolationLevel = IsolationLevel.Serializable
            };

            options.ProviderName = "InMemory";
            options.Configuration = configuration;
        }
    }
}
