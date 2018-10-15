using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql
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
        Task<IEnumerable<T>> GetAsync<T>(int[] ids) where T : class;

        IQuery Query();

        IQuery<T> ExecuteQuery<T>(ICompiledQuery<T> compiledQuery) where T : class;
        
        /// <summary>
        /// Cancels any pending command
        /// </summary>
        void Cancel();

        /// <summary>
        /// Flushes the current commands asynchronously.
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// Returns a <see cref="DbTransaction"/> that is used by this instance.
        /// </summary>
        IDbTransaction Demand();

        /// <summary>
        /// Registers index providers.
        /// </summary>
        /// <param name="indexProviders">The index providers to register.</param>
        /// <returns>The <see cref="ISession"/> instance.</returns>
        ISession RegisterIndexes(params IIndexProvider[] indexProviders);

        IStore Store { get; }
    }

    public static class SessionExtensions
    {
        public async static Task<T> GetAsync<T>(this ISession session, int id) where T : class
        {
            return (await session.GetAsync<T>(new[] { id })).FirstOrDefault();
        }
    }
}
