using Microsoft.Data.Sqlite;
using System;
using System.Data;
using YesSql.Core.Services;
using YesSql.Storage.InMemory;

namespace YesSql.Provider.InMemory
{
    public static class InMemoryDbProviderOptionsExtensions
    {
        private const string ConnectionString = "Data Source=:memory:";

        public static Configuration UseInMemory(
            this Configuration configuration)
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
