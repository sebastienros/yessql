using YesSql.Core.Storage;

namespace YesSql.Storage.FileSystem
{
    public class FileSystemDocumentStorageFactory : IDocumentStorageFactory
    {
        private readonly string _root;
        private FileSystemDocumentStorage _storage;

        public FileSystemDocumentStorageFactory(string root)
        {
            _root = root;
        }
        public IDocumentStorage CreateDocumentStorage()
        {
            if(_storage == null)
            {
                _storage = new FileSystemDocumentStorage(_root);
            }

            return _storage;
        }
    }
}
