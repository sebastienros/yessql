using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Core.Storage;

namespace YesSql.Storage.Cache
{
    public class CacheDocumentStorage : IDocumentStorage
    {
        public ConcurrentDictionary<int, string> _documents;
        private readonly static JsonSerializerSettings _jsonSettings;

        static CacheDocumentStorage()
        {
            _jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
        }

        private readonly IDocumentStorage _concreteDocumentStorage;

        public CacheDocumentStorage(ConcurrentDictionary<int, string> documents, IDocumentStorage concreteDocumentStorage)
        {
            _documents = documents;
            _concreteDocumentStorage = concreteDocumentStorage;
        }

        public Task CreateAsync(params IIdentityEntity[] documents)
        {
            foreach (var document in documents)
            {
                _documents[document.Id] = JsonConvert.SerializeObject(document.Entity, _jsonSettings);
            }

            return _concreteDocumentStorage.CreateAsync(documents);
        }

        public Task UpdateAsync(params IIdentityEntity[] documents)
        {
            foreach (var document in documents)
            {
                _documents[document.Id] = JsonConvert.SerializeObject(document.Entity, _jsonSettings);
            }

            return _concreteDocumentStorage.UpdateAsync(documents);
        }

        public Task DeleteAsync(params IIdentityEntity[] documents)
        {
            if (documents == null)
            {
                throw new ArgumentException("Can't delete a document with a null id");
            }

            foreach (var document in documents)
            {
                _documents.TryRemove(document.Id, out string value);
            }

            return _concreteDocumentStorage.DeleteAsync(documents);
        }

        public async Task<IEnumerable<T>> GetAsync<T>(params int[] ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException("id");
            }

            var result = new List<T>();
            foreach (var id in ids)
            {
                if (_documents.TryGetValue(id, out string document))
                {
                    result.Add(JsonConvert.DeserializeObject<T>(document, _jsonSettings));
                }
                else
                {
                    var concreteResults = await _concreteDocumentStorage.GetAsync<T>(ids);
                    foreach (var concreteResult in concreteResults)
                    {
                        result.Add(concreteResult);
                        _documents[id] = JsonConvert.SerializeObject(concreteResult, _jsonSettings);
                    }

                    return concreteResults;
                }
            }

            return result;
        }

        public async Task<IEnumerable<object>> GetAsync(params IIdentityEntity[] documents)
        {
            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            var result = new List<object>();
            foreach (var document in documents)
            {
                if (_documents.TryGetValue(document.Id, out string content))
                {
                    result.Add(JsonConvert.DeserializeObject(content, _jsonSettings));
                }
                else
                {
                    var concreteResults = await _concreteDocumentStorage.GetAsync(documents);
                    foreach (var concreteResult in concreteResults)
                    {
                        result.Add(concreteResult);
                        _documents[document.Id] = JsonConvert.SerializeObject(concreteResult, _jsonSettings);
                    }

                    return concreteResults;
                }
            }

            return result;
        }
    }
}
