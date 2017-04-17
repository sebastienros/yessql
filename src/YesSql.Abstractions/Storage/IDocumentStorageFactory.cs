using System.Threading.Tasks;
using YesSql;

namespace YesSql.Storage
{
    public interface IDocumentStorageFactory
    {
        /// <summary>
        /// Creates a new storage instance that is not shared accross clients.
        /// </summary>
        IDocumentStorage CreateDocumentStorage(ISession session, IConfiguration configuration);

        /// <summary>
        /// Initializes the storage, for instance creating required SQL tables.
        /// </summary>
        Task InitializeAsync(IConfiguration configuration);

        /// <summary>
        /// Initializes the storage , for instance creating required SQL tables.
        /// </summary>
        Task InitializeCollectionAsync(IConfiguration configuration, string collectionName);

    }
}