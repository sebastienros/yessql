using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql
{
    public interface IStore : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="ISession"/> to communicate with the <see cref="IStore"/> with
        /// the specified <see cref="IsolationLevel"/>.
        /// </summary>
        ISession CreateSession(IsolationLevel isolationLevel);

        /// <summary>
        /// Registers index providers.
        /// </summary>
        /// <param name="indexProviders">The index providers to register.</param>
        /// <param name="collection">The name of the collection.</param>
        /// <returns>The <see cref="IStore"/> instance.</returns>
        IStore RegisterIndexes(IEnumerable<IIndexProvider> indexProviders, string collection = null);

        /// <summary>
        /// Returns the <see cref="IConfiguration" /> instance used to create this store.
        /// </summary>
        IConfiguration Configuration { get; }

        /// <summary>
        /// Initializes the database by creating the required tables and the default collection if necessary.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Initializes a collection in the database by creating the required tables if necessary.
        /// </summary>
        Task InitializeCollectionAsync(string collection);

        /// <summary>
        /// Create an instance of <see cref="IEnumerable&lt;IndexDescriptor&gt;" /> containing descriptors for all indexes associated to a type and a collection.
        /// </summary>        
        IEnumerable<IndexDescriptor> Describe(Type target, string collection = null);

        /// <summary>
        /// Returns the <see cref="ISqlDialect" /> instance used to create this store.
        /// </summary>
        ISqlDialect Dialect { get; }

        /// <summary>
        /// Returns the <see cref="ITypeService" /> instance used to create this store.
        /// </summary>
        ITypeService TypeNames { get; }
    }

    public static class IStoreExtensions
    {
        /// <summary>
        /// Creates a new <see cref="ISession"/> to communicate with the <see cref="IStore"/> with
        /// the default <see cref="IsolationLevel"/>.
        /// </summary>
        public static ISession CreateSession(this IStore store)
        {
            return store.CreateSession(store.Configuration.IsolationLevel);
        }
    }
}
