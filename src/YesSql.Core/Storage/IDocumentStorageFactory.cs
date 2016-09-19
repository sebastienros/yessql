using System.Threading.Tasks;
using YesSql.Core.Services;

namespace YesSql.Core.Storage
{
    public interface IDocumentStorageFactory
    {
        /// <summary>
        /// Creates a new storage instance that is not shared accross clients.
        /// </summary>
        IDocumentStorage CreateDocumentStorage(ISession session, Configuration configuration);

        /// <summary>
        /// Initializes the storage, for instance creating required SQL tables.
        /// </summary>
        Task InitializeAsync(Configuration configuration);
    }
}