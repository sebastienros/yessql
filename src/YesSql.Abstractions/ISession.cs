using System;
using System.Collections.Generic;
using System.Data.Common;
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
        /// <param name="obj">The entity to save.</param>
        /// <param name="checkConcurrency">If true, a <see cref="ConcurrencyException"/> is thrown if the entity has been updated concurrently by another session.</param>
        /// <param name="collection">The name of the collection to store the object in.</param>
        void Save(object obj, bool checkConcurrency = false, string collection = null);

        /// <summary>
        /// Deletes an object and its indexes from the store.
        /// </summary>
        void Delete(object item, string collection = null);

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
        bool Import(object item, int id, int version, string collection = null);

        /// <summary>
        /// Removes an item from the identity map.
        /// </summary>
        /// <remarks>
        /// This method can be used to remove an item that should not be served again from the cache.
        /// For instance when its state as changed and any subsequent query should not return the 
        /// modified instance but a fresh one.
        /// </remarks>
        void Detach(object item, string collection = null);

        /// <summary>
        /// Loads objects by id.
        /// </summary>
        /// <returns>A collection of objects in the same order they were defined.</returns>
        Task<IEnumerable<T>> GetAsync<T>(int[] ids, string collection = null) where T : class;

        /// <summary>
        /// Creates a new <see cref="IQuery"/> object.
        /// </summary>
        IQuery Query(string collection = null);

        /// <summary>
        /// Executes a compiled query.
        /// </summary>
        /// <remarks>
        /// A compiled query is an instance of a class implementing <see cref="ICompiledQuery{T}" />.
        /// Compiled queries allow YesSql to cache the SQL statement that would be otherwise generated
        /// on each invocation of the LINQ query. 
        /// </remarks>
        IQuery<T> ExecuteQuery<T>(ICompiledQuery<T> compiledQuery, string collection = null) where T : class;

        /// <summary>
        /// Cancels any pending commands.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Flushes pending commands to the database.
        /// </summary>
        /// <remarks>
        /// This doesn't commit or dispose of the transaction. A call to <see cref="CommitAsync"/>
        /// is still necessary for the changes to be visible from other transactions.
        /// </remarks>
        Task FlushAsync();

        /// <summary>
        /// Flushes any changes, commits the transaction, and disposes it.
        /// </summary>
        /// <remarks>
        /// Sessions are automatically committed when disposed, however calling <see cref="CommitAsync"/>
        /// is recommended before the session is disposed to prevent it from being called on a non-async
        /// code path.
        /// </remarks>
        Task CommitAsync();

        /// <summary>
        /// Returns a <see cref="DbTransaction"/> that is used by this instance.
        /// </summary>
        Task<DbTransaction> DemandAsync();

        /// <summary>
        /// Registers index providers that are used only during the lifetime of this session.
        /// </summary>
        /// <param name="indexProviders">The index providers to register.</param>
        /// <param name="collection">The name of the collection to store the object in.</param>
        /// <returns>The <see cref="ISession"/> instance.</returns>
        ISession RegisterIndexes(IIndexProvider[] indexProviders, string collection = null);

        /// <summary>
        /// Gets the <see cref="Store" /> instance that created this session. 
        /// </summary>
        IStore Store { get; }
    }

    public static class SessionExtensions
    {
        /// <summary>
        /// Loads an object by its id.
        /// </summary>
        /// <returns>The object or <c>null</c>.</returns>
        public async static Task<T> GetAsync<T>(this ISession session, int id, string collection = null) where T : class
        {
            return (await session.GetAsync<T>(new[] { id }, collection)).FirstOrDefault();
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
        public static bool Import(this ISession session, object item, string collection = null)
        {
            return session.Import(item, 0, 0, collection);
        }

        /// <summary>
        /// Registers index providers that are used only during the lifetime of this session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="indexProviders">The index providers to register.</param>
        /// <returns>The <see cref="ISession"/> instance.</returns>
        public static ISession RegisterIndexes(this ISession session, params IIndexProvider[] indexProviders)
        {
            return session.RegisterIndexes(indexProviders, null);
        }

        /// <summary>
        /// Registers index providers that are used only during the lifetime of this session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="indexProvider">The index provider to register.</param>
        /// <param name="collection">The name of the collection.</param>
        /// <returns>The <see cref="ISession"/> instance.</returns>
        public static ISession RegisterIndexes(this ISession session, IIndexProvider indexProvider, string collection = null)
        {
            return session.RegisterIndexes(new[] { indexProvider }, collection);
        }

        /// <summary>
        /// Saves a new or existing object to the store, and updates
        /// the corresponding indexes.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="obj">The entity to save.</param>
        /// <param name="collection">The name of the collection.</param>
        public static void Save(this ISession session, object obj, string collection = null)
        {
            session.Save(obj, false, collection);
        }
    }
}
