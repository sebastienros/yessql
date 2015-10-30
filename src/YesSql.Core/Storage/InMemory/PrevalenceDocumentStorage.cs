using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YesSql.Core.Storage.InMemory
{
    public class PrevalenceDocumentStorage : IDocumentStorage
    {
        public Dictionary<int, object> _documents = new Dictionary<int, object>();

        public PrevalenceDocumentStorage()
        {
        }

        public Task SaveAsync<T>(int id, T item)
        {
            _documents[id] = item;

            return Task.FromResult(0);
        }
        
        public Task DeleteAsync(int documentId)
        {
            if (documentId == 0)
            {
                throw new ArgumentException("Can't delete a document with a null id");
            }

            _documents.Remove(documentId);

            return Task.FromResult(0);
        }

        public Task<IEnumerable<T>> GetAsync<T>(IEnumerable<int> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException("id");
            }

            var result = new List<T>();
            foreach (var id in ids)
            {
                result.Add((T)_documents[id]);
            }

            return Task.FromResult((IEnumerable<T>)result);
        }

        public Task<IEnumerable<object>> GetAsync(IEnumerable<int> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException("id");
            }

            var result = new List<object>();
            foreach (var id in ids)
            {
                result.Add(_documents[id]);
            }

            return Task.FromResult((IEnumerable<object>)result);
        }
    }
}
