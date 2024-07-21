using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql
{
    /// <summary>
    /// Represents a connection to the document store.
    /// </summary>
    public interface ISession : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Saves a new or existing object to the store, and updates
        /// the corresponding indexes.
        /// </summary>
        /// <param name="obj">The entity to save.</param>
        /// <param name="checkConcurrency">If true, a <see cref="ConcurrencyException"/> is thrown if the entity has been updated concurrently by another session.</param>
        /// <param name="collection">The name of the collection to store the object in.</param>
        [Obsolete($"Instead, utilize the {nameof(SaveAsync)} method. This current method is slated for removal in upcoming releases.")]
        void Save(object obj, bool checkConcurrency = false, string collection = null);


        /// <summary>
        /// Saves a new or existing object to the store, and updates
        /// the corresponding indexes.
        /// </summary>
        /// <param name="obj">The entity to save.</param>
        /// <param name="checkConcurrency">If true, a <see cref="ConcurrencyException"/> is thrown if the entity has been updated concurrently by another session.</param>
        /// <param name="collection">The name of the collection to store the object in.</param>
        Task SaveAsync(object obj, bool checkConcurrency = false, string collection = null);

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
        bool Import(object item, long id, long version, string collection = null);

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
        Task<IEnumerable<T>> GetAsync<T>(long[] ids, string collection = null) where T : class;

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
        Task CancelAsync();

        /// <summary>
        /// Flushes pending commands to the database.
        /// </summary>
        /// <remarks>
        /// This doesn't commit or dispose of the transaction. A call to <see cref="SaveChangesAsync"/>
        /// is still necessary for the changes to be visible from other transactions.
        /// </remarks>
        Task FlushAsync();

        /// <summary>
        /// Flushes any changes, commits the transaction, and disposes the transaction.
        /// </summary>
        /// <remarks>
        /// Sessions are not automatically committed when disposed, and <see cref="SaveChangesAsync"/>
        /// must be called before disposing the <see cref="ISession"/>
        /// </remarks>
        Task SaveChangesAsync();

        /// <summary>
        /// Creates or returns a <see cref="DbConnection"/>.
        /// </summary>
        Task<DbConnection> CreateConnectionAsync();

        /// <summary>
        /// Creates or returns an existing <see cref="DbTransaction"/> with the default isolation level.
        /// </summary>
        Task<DbTransaction> BeginTransactionAsync();

        /// <summary>
        /// Creates or returns an existing <see cref="DbTransaction"/> with the specified isolation level.
        /// </summary>
        Task<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel);

        /// <summary>
        /// Returns the current <see cref="DbTransaction"/> if it exists.
        /// </summary>
        DbTransaction CurrentTransaction { get; }

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

        IEnumerable<IndexDescriptor> ExtraIndexDescriptors { get; set; }
    }
}
