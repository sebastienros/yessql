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

        public Task CreateAsync(params IIdentityEntity[] documents)
        {
            foreach (var document in documents)
            {
                _documents[document.Id] = JsonConvert.SerializeObject(document.Entity, _jsonSettings);
            }

#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        public Task UpdateAsync(params IIdentityEntity[] documents)
        {
            return CreateAsync(documents);
        }

        public Task DeleteAsync(params IIdentityEntity[] documents)
        {
            if (documents == null)
            {
                throw new ArgumentException("Can't delete a document with a null id");
            }

            foreach(var document in documents)
            {
                _documents.Remove(document.Id);
            }

#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        public Task<IEnumerable<T>> GetAsync<T>(params int[] ids)
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

        public Task<IEnumerable<object>> GetAsync(params IIdentityEntity[] documents)
        {
            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            var result = new List<object>();
            foreach (var document in documents)
            {
                string content; ;
                if (_documents.TryGetValue(document.Id, out content))
                {
                    result.Add(JsonConvert.DeserializeObject(content, _jsonSettings));
                }
            }

            return Task.FromResult((IEnumerable<object>)result);
        }
    }
}
