using System.Threading.Tasks;

namespace YesSql.Core.Storage
{
    public interface IDocumentStorageFactory
    {
        /// <summary>
        /// Creates a new storage instance that is not shared accross clients.
        /// </summary>
        IDocumentStorage CreateDocumentStorage();

        /// <summary>
        /// Initializes the storage, for instance creating required SQL tables.
        /// </summary>
        Task InitializeAsync();
    }
}