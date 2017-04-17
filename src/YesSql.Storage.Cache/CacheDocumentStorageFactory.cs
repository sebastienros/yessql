using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Services;
using YesSql.Storage;

namespace YesSql.Storage.Cache
{
    public class CacheDocumentStorageFactory : IDocumentStorageFactory
    {
        private ConcurrentDictionary<int, string> _documents = new ConcurrentDictionary<int, string>();
        private readonly IDocumentStorageFactory _concreteStorageFactory;

        public CacheDocumentStorageFactory(IDocumentStorageFactory concreteStorageFactory)
        {
            _concreteStorageFactory = concreteStorageFactory;
        }

        public IDocumentStorage CreateDocumentStorage(ISession session, IConfiguration configuration)
        {
            return new CacheDocumentStorage(_documents, _concreteStorageFactory.CreateDocumentStorage(session, configuration));
        }

        public Task InitializeAsync(IConfiguration configuration)
        {
            return _concreteStorageFactory.InitializeAsync(configuration);
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
