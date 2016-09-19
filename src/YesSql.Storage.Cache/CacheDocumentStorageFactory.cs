using System.Threading.Tasks;
using YesSql.Core.Services;
using YesSql.Core.Storage;

namespace YesSql.Storage.Cache
{
    public class CacheDocumentStorageFactory : IDocumentStorageFactory
    {
        private CacheDocumentStorage _storage;
        private readonly IDocumentStorageFactory _concreteStorageFactory;

        public CacheDocumentStorageFactory(IDocumentStorageFactory concreteStorageFactory)
        {
            _concreteStorageFactory = concreteStorageFactory;
        }

        public IDocumentStorage CreateDocumentStorage(ISession session, Configuration configuration)
        {
            if(_storage == null)
            {
                _storage = new CacheDocumentStorage(_concreteStorageFactory.CreateDocumentStorage(session, configuration));
            }

            return _storage;
        }

        public Task InitializeAsync(Configuration configuration)
        {
            return _concreteStorageFactory.InitializeAsync(configuration);
        }

    }
}
