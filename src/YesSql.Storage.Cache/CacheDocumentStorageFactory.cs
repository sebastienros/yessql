using System.Threading.Tasks;
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

        public IDocumentStorage CreateDocumentStorage()
        {
            if(_storage == null)
            {
                _storage = new CacheDocumentStorage(_concreteStorageFactory.CreateDocumentStorage());
            }

            return _storage;
        }

        public Task InitializeAsync()
        {
            return _concreteStorageFactory.InitializeAsync();
        }

    }
}
