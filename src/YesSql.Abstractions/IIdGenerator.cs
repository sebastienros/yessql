using System.Data;
using System.Threading.Tasks;
using YesSql.Sql;

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
        Task InitializeAsync(IStore store, ISchemaBuilder builder);

        /// <summary>
        /// Generates a unique identifier for the store.
        /// </summary>
        /// <param name="transaction">The currently used transaction.</param>
        /// <param name="collection">The name of the collection to generate the identifier for.</param>
        /// <returns>A unique identifier</returns>
        long GetNextId(IDbTransaction transaction, string collection);
    }
}