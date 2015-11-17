using YesSql.Core.Storage;

namespace YesSql.Storage.Prevalence
{
    public class PrevalenceDocumentStorageFactory : IDocumentStorageFactory
    {
        private PrevalenceDocumentStorage _storage;

        public IDocumentStorage CreateDocumentStorage()
        {
            if(_storage == null)
            {
                _storage = new PrevalenceDocumentStorage();
            }

            return _storage;
        }
    }
}
