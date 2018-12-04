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
        /// Deletes an object and its indexes from the store.
        /// </summary>
        void Delete(object item);

        /// <summary>
        /// Imports an object in the local identity map.
        /// </summary>
        /// <remarks>
        /// This method can be used to re-attach an object that exists in the database
        /// but was not loaded from this session, or has been duplicated. If not imported
        /// in a session a duplicate record would tentatively be created in the database
        /// and a duplicate primary key constraint would fail.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the object was imported, <c>false</c> otherwise.
        /// </returns>
        bool Import(object item, int id);

        /// <summary>
        /// Loads objects by id.
        /// </summary>
        /// <returns>A collection of objects in the same order they were defined.</returns>
        Task<IEnumerable<T>> GetAsync<T>(int[] ids) where T : class;

        /// <summary>
        /// Creates a new <see cref="IQuery"/> object.
        /// </summary>
        /// <returns></returns>
        IQuery Query();

        IQuery<T> ExecuteQuery<T>(ICompiledQuery<T> compiledQuery) where T : class;
        
        /// <summary>
        /// Cancels any pending command.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Flushes the current commands asynchronously.
        /// </summary>
        /// <remarks>
        /// Sessions are automatically flushed when disposed, however calling <see cref="CommitAsync"/>
        /// is recommended before the session is disposed to prevent it from being called on a non-async
        /// code path.
        /// </remarks>
        Task CommitAsync();

        /// <summary>
        /// Returns a <see cref="DbTransaction"/> that is used by this instance.
        /// </summary>
        Task<IDbTransaction> DemandAsync();

        /// <summary>
        /// Registers index providers that are used only during the lifetime of this session.
        /// </summary>
        /// <param name="indexProviders">The index providers to register.</param>
        /// <returns>The <see cref="ISession"/> instance.</returns>
        ISession RegisterIndexes(params IIndexProvider[] indexProviders);

        IStore Store { get; }
    }

    public static class SessionExtensions
    {
        /// <summary>
        /// Loads an object by its id.
        /// </summary>
        /// <returns>The object or <c>null</c>.</returns>
        public async static Task<T> GetAsync<T>(this ISession session, int id) where T : class
        {
            return (await session.GetAsync<T>(new[] { id })).FirstOrDefault();
        }

        /// <summary>
        /// Imports an object in the local identity map.
        /// </summary>
        /// <remarks>
        /// This method can be used to re-attach an object that exists in the database
        /// but was not loaded from this session, or has been duplicated. If not imported
        /// in a session a duplicate record would tentatively be created in the database
        /// and a duplicate primary key constraint would fail.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the object was imported, <c>false</c> otherwise.
        /// </returns>
        public static bool Import(this ISession session, object item)
        {
            return session.Import(item, 0);
        }
    }
}
