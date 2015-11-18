using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Core.Storage;

namespace YesSql.Storage.Prevalence
{
    public class PrevalenceDocumentStorage : IDocumentStorage
    {
        public Dictionary<int, object> _documents = new Dictionary<int, object>();

        public PrevalenceDocumentStorage()
        {
        }

        public Task CreateAsync<T>(int[] ids, T[] items)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                _documents[ids[i]] = items[i];
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

            for (var i = 0; i < ids.Length; i++)
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
