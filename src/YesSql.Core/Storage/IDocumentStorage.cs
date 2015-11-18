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
        Task CreateAsync<T>(int[] ids, T[] items);

        /// <summary>
        /// Updates a document in the store.
        /// </summary>
        Task UpdateAsync<T>(int[] ids, T[] items);

        /// <summary>
        /// Deletes a document from the store.
        /// </summary>
        Task DeleteAsync(int[] ids);

        /// <summary>
        /// Loads a document by its id
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<T>> GetAsync<T>(IEnumerable<int> ids);

        /// <summary>
        /// Loads a document by its id
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<object>> GetAsync(IEnumerable<int> ids);
    }

    public static class StorageExtensions
    {
        public static async Task<object> GetAsync(this IDocumentStorage storage, int id)
        {
            return (await storage.GetAsync(new[] { id }))?.FirstOrDefault();
        }

        public static async Task<T> GetAsync<T>(this IDocumentStorage storage, int id) where T : class
        {
            return (await storage.GetAsync<T>(new[] { id }))?.FirstOrDefault();
        }

        /// <summary>
        /// Updates a document in to the store.
        /// </summary>
        public static Task CreateAsync<T>(this IDocumentStorage storage, int id, T item)
        {
            return storage.CreateAsync<T>(new int[] { id }, new T[] { item });
        }

        /// <summary>
        /// Updates a document in to the store.
        /// </summary>
        public static Task UpdateAsync<T>(this IDocumentStorage storage, int id, T item)
        {
            return storage.UpdateAsync<T>(new int[] { id }, new T[] { item });
        }

        /// <summary>
        /// Deletes a document from the store.
        /// </summary>
        public static Task DeleteAsync(this IDocumentStorage storage, int id)
        {
            return storage.DeleteAsync(new int[] { id });
        }


    }
}
