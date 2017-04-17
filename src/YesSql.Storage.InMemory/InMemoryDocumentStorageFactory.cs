using System;
using System.Threading.Tasks;
using YesSql.Services;
using YesSql.Storage;

namespace YesSql.Storage.InMemory
{
    public class InMemoryDocumentStorageFactory : IDocumentStorageFactory
    {
        private InMemoryDocumentStorage _storage;

        public IDocumentStorage CreateDocumentStorage(ISession session, IConfiguration configuration)
        {
            if (_storage == null)
            {
                _storage = new InMemoryDocumentStorage();
            }

            return _storage;
        }

        public Task InitializeAsync(IConfiguration configuration)
        {
#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        public Task InitializeCollectionAsync(IConfiguration configuration, string collectionName)
        {
#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
