using LightningDB;
using System;
using YesSql.Core.Storage;

namespace YesSql.Storage.LightningDB
{
    public class LightningDocumentStorageFactory : IDocumentStorageFactory, IDisposable
    {
        private readonly string _rootFolder;
        private readonly LightningEnvironment _env;

        public LightningDocumentStorageFactory(string rootFolder)
        {
            _rootFolder = rootFolder;
            _env = new LightningEnvironment(rootFolder);
            _env.Open();
        }
        public IDocumentStorage CreateDocumentStorage()
        {
            return new LightningDocumentStorage(_env);
        }

        public void Dispose()
        {
            _env.Dispose();
        }
    }
}
