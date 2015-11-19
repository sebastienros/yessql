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
            _jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
        }

        public InMemoryDocumentStorage()
        {
        }

        public Task CreateAsync<T>(int[] ids, T[] items)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                _documents[ids[i]] = JsonConvert.SerializeObject(items[i], _jsonSettings);
            }

            return Task.CompletedTask;
        }

        public Task UpdateAsync<T>(int[] ids, T[] items)
        {
            return CreateAsync(ids, items);
        }

        public Task DeleteAsync(int[] ids)
        {
            if (ids == null)
            {
                throw new ArgumentException("Can't delete a document with a null id");
            }

            for(var i=0; i<ids.Length; i++)
            {
                _documents.Remove(ids[i]);
            }

            return Task.CompletedTask;
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
