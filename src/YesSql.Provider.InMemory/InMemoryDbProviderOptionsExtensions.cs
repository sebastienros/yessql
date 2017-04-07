using Microsoft.Data.Sqlite;
using System;
using System.Data;
using YesSql.Core.Services;
using YesSql.Storage.Cache;
using YesSql.Storage.InMemory;

namespace YesSql.Provider.InMemory
{
    public static class InMemoryDbProviderOptionsExtensions
    {
        private const string ConnectionString = "Data Source=:memory:";

        public static void UseInMemory(
            this Configuration configuration)
        {
            UseInMemory(configuration);
        }

        public static void UseInMemory(
            this Configuration configuration,
            bool cached = false)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.ConnectionFactory = new DbConnectionFactory<SqliteConnection>(ConnectionString);
            configuration.IsolationLevel = IsolationLevel.Serializable;

            if (cached)
            {
                configuration.DocumentStorageFactory = 
                    new CacheDocumentStorageFactory(new InMemoryDocumentStorageFactory());
            }
            else
            {
                configuration.DocumentStorageFactory = new InMemoryDocumentStorageFactory();
            }
        }
    }
}
