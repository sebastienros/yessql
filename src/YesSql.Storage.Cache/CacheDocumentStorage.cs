using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Core.Storage;
using YesSql.Core.Services;

namespace YesSql.Storage.Cache
{
    public class CacheDocumentStorage : IDocumentStorage
    {
        public Dictionary<int, string> _documents = new Dictionary<int, string>();
        private readonly static JsonSerializerSettings _jsonSettings;

        static CacheDocumentStorage()
        {
            _jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
        }

        private readonly IDocumentStorage _concreteDocumentStorage;

        public ISession Session { get; set; }

        public CacheDocumentStorage(IDocumentStorage concreteDocumentStorage)
        {
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

            foreach(var document in documents)
            {
                _documents.Remove(document.Id);
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
                string document;
                if (_documents.TryGetValue(id, out document))
                {
                    result.Add(JsonConvert.DeserializeObject<T>(document, _jsonSettings));
                }
                else
                {
                    var concreteResults = await _concreteDocumentStorage.GetAsync<T>(ids);
                    foreach(var concreteResult in concreteResults)
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
                string content; ;
                if (_documents.TryGetValue(document.Id, out content))
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
