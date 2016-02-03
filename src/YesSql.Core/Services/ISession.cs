using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Core.Query;

namespace YesSql.Core.Services
{
    /// <summary>
    /// Represents a connection to the document store.
    /// </summary>
    public interface ISession : IDisposable
    {
        /// <summary>
        /// Saves a new or existing object to the store, and updates
        /// the corresponding indexes.
        /// </summary>
        void Save(object obj);

        /// <summary>
        /// Deletes an object from the store and its indexes.
        /// </summary>
        void Delete(object item);

        /// <summary>
        /// Loads objects by id
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<T>> GetAsync<T>(IEnumerable<int> ids) where T : class;
                        
        IQuery QueryAsync();

        /// <summary>
        /// Cancels any pending command
        /// </summary>
        void Cancel();

        /// <summary>
        /// Commits the current transaction asynchronously
        /// </summary>
        /// <param name="keepTracked">
        /// <c>True</c> if the tracked entities should still be tracked. This
        /// parameter should normally not be used.
        /// </param>
        Task CommitAsync(bool keepTracked = false);
    }

    public static class SessionExtensions
    {
        public async static Task<T> GetAsync<T>(this ISession session, int id) where T : class
        {
            return (await session.GetAsync<T>(new[] { id })).FirstOrDefault();
        }
    }
}
