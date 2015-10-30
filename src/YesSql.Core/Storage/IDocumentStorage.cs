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
        /// Saves a document or its modifications to the store.
        /// </summary>
        Task SaveAsync<T>(int id, T item);

        /// <summary>
        /// Deletes a document from the store.
        /// </summary>
        Task DeleteAsync(int id);

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
            return (await storage.GetAsync(new[] { id })).FirstOrDefault();
        }

        public static async Task<T> GetAsync<T>(this IDocumentStorage storage, int id) where T : class
        {
            return (await storage.GetAsync<T>(new[] { id })).FirstOrDefault();
        }
    }
}
