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

        public static void UseInMemory(
            this Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration = new Configuration
            {
                ConnectionFactory = new DbConnectionFactory<SqliteConnection>(ConnectionString),
                DocumentStorageFactory = new InMemoryDocumentStorageFactory(),
                IsolationLevel = IsolationLevel.Serializable
            };
        }
    }
}
