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

        public Task CreateAsync(params IIdentityEntity[] documents)
        {
            foreach(var document in documents)
            {
                _documents[document.Id] = document.Entity;
            }

            return Task.CompletedTask;
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

            return Task.CompletedTask;
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
                result.Add((T)_documents[id]);
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
                result.Add(_documents[document.Id]);
            }

            return Task.FromResult((IEnumerable<object>)result);
        }
    }
}
