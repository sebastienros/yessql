using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Core.Storage;

namespace YesSql.Storage.InMemory
{
    public class InMemoryDocumentStorage : IDocumentStorage
    {
        public Dictionary<int, string> _documents = new Dictionary<int, string>();
        private readonly static JsonSerializerSettings _jsonSettings;

        static InMemoryDocumentStorage()
        {
            _jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        }

        public InMemoryDocumentStorage()
        {
        }

        public Task SaveAsync<T>(int id, T item)
        {
            _documents[id] = JsonConvert.SerializeObject(item, _jsonSettings);

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
                string document;
                if (_documents.TryGetValue(id, out document))
                {
                    result.Add(JsonConvert.DeserializeObject<T>(document, _jsonSettings));
                }
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
                string document; ;
                if (_documents.TryGetValue(id, out document))
                {
                    result.Add(JsonConvert.DeserializeObject(document, _jsonSettings));
                }
            }

            return Task.FromResult((IEnumerable<object>)result);
        }
    }
}
