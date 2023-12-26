using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Commands;

namespace YesSql
{
    public interface IDocumentChangedEventHandler
    {
        Task<IEnumerable<IExternalCommand>> CreatedAsync(Document document, object entity);
        Task<IEnumerable<IExternalCommand>> DeletedAsync(Document document, object entity);
        Task<IEnumerable<IExternalCommand>> UpdatedAsync(Document document, object entity);
    }
}
