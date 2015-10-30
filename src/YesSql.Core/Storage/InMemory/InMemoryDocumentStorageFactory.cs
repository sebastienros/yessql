namespace YesSql.Core.Storage.InMemory
{
    public class InMemoryDocumentStorageFactory : IDocumentStorageFactory
    {
        private InMemoryDocumentStorage _storage;

        public IDocumentStorage CreateDocumentStorage()
        {
            if(_storage == null)
            {
                _storage = new InMemoryDocumentStorage();
            }

            return _storage;
        }
    }
}
