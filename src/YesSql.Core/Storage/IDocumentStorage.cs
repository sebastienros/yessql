using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YesSql.Core.Storage
{
    /// <summary>
    /// Reprensents a component responsible for storing <see cref="Document"/>
    /// instances in a persistent medium.
    /// </summary>
    public interface IDocumentStorage
    {
        /// <summary>
        /// Creates a document in the store.
        /// </summary>
        Task CreateAsync(params IIdentityEntity[] documents);

        /// <summary>
        /// Updates a document in the store.
        /// </summary>
        Task UpdateAsync(params IIdentityEntity[] documents);

        /// <summary>
        /// Deletes a document from the store.
        /// </summary>
        Task DeleteAsync(params IIdentityEntity[] documents);

        /// <summary>
        /// Loads a document by its id
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<T>> GetAsync<T>(params int[] ids);

        /// <summary>
        /// Loads a document by its id
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<object>> GetAsync(params IIdentityEntity[] documents);
    }

    public static class StorageExtensions
    {
        public static async Task<object> GetAsync(this IDocumentStorage storage, int id, Type type)
        {
            return (await storage.GetAsync(new IdentityDocument(id, type)))?.FirstOrDefault();
        }

        public static async Task<T> GetAsync<T>(this IDocumentStorage storage, int id) where T : class
        {
            return (await storage.GetAsync<T>(id))?.FirstOrDefault();
        }

        /// <summary>
        /// Creates a document in to the store.
        /// </summary>
        public static Task CreateAsync(this IDocumentStorage storage, int id, object item)
        {
            return storage.CreateAsync(new IdentityDocument(id, item));
        }

        /// <summary>
        /// Updates a document in to the store.
        /// </summary>
        public static Task UpdateAsync(this IDocumentStorage storage, int id, object item)
        {
            return storage.UpdateAsync(new IdentityDocument(id, item));
        }

        /// <summary>
        /// Deletes a document from the store.
        /// </summary>
        public static Task DeleteAsync(this IDocumentStorage storage, int id, Type type)
        {
            return storage.DeleteAsync(new IdentityDocument(id, type));
        }
    }
}
