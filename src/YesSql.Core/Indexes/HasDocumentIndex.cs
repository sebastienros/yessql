using YesSql.Core.Data.Models;

namespace YesSql.Core.Indexes
{
    public interface IHasDocumentIndex : IIndex
    {
        Document Document { get; set; }
    }
}
