using System;
using System.Threading;
using System.Threading.Tasks;

namespace YesSql
{
    /// <summary>
    /// Represents a component that is able to generate unique identifiers for new documents.
    /// Any implementation must be thread-safe.
    /// </summary>
    public interface IIdGenerator
    {
        /// <summary>
        /// Invoked when the underlying store is created.
        /// </summary>
        /// <param name="store">The store that this <see cref="IIdGenerator"/> instance is assigned to.</param>
        Task InitializeAsync(IStore store, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoked when the underlying store is created.
        /// </summary>
        /// <param name="store">The store that this <see cref="IIdGenerator"/> instance is assigned to.</param>
        [Obsolete($"Instead, utilize the {nameof(InitializeAsync)} method with a CancellationToken parameter. This current method is slated for removal in upcoming releases.")]
        Task InitializeAsync(IStore store);

        /// <summary>
        /// Initializes a document collection.
        /// </summary>
        Task InitializeCollectionAsync(IConfiguration configuration, string collection, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initializes a document collection.
        /// </summary>
        [Obsolete($"Instead, utilize the {nameof(InitializeCollectionAsync)} method with a CancellationToken parameter. This current method is slated for removal in upcoming releases.")]
        Task InitializeCollectionAsync(IConfiguration configuration, string collection);

        /// <summary>
        /// Generates a unique identifier for the store.
        /// </summary>
        /// <param name="collection">The name of the collection to generate the identifier for.</param>
        /// <returns>A unique identifier</returns>
        [Obsolete($"Instead, utilize the {nameof(GetNextIdAsync)} method. This current method is slated for removal in upcoming releases.")]
        long GetNextId(string collection);

        /// <summary>
        /// Generates a unique identifier for the store.
        /// </summary>
        /// <param name="collection">The name of the collection to generate the identifier for.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unique identifier</returns>
        Task<long> GetNextIdAsync(string collection, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a unique identifier for the store.
        /// </summary>
        /// <param name="collection">The name of the collection to generate the identifier for.</param>
        /// <returns>A unique identifier</returns>
        [Obsolete($"Instead, utilize the {nameof(GetNextIdAsync)} method with a CancellationToken parameter. This current method is slated for removal in upcoming releases.")]
        Task<long> GetNextIdAsync(string collection);
    }
}
