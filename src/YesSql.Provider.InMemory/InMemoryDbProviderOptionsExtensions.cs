using Microsoft.Data.Sqlite;
using System;
using System.Data;
using YesSql.Storage;
using YesSql.Storage.InMemory;
using YesSql;

namespace YesSql.Provider.InMemory
{
    public static class InMemoryDbProviderOptionsExtensions
    {
        private const string ConnectionString = "Data Source=:memory:";

        public static IConfiguration UseInMemory(
            this IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.ConnectionFactory = new DbConnectionFactory<SqliteConnection>(ConnectionString);
            configuration.DocumentStorageFactory = new InMemoryDocumentStorageFactory();
            configuration.IsolationLevel = IsolationLevel.Serializable;

            return configuration;
        }
    }
}
