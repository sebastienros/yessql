using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Commands;

namespace YesSql.Provider
{
    public class DocumentChangedEventHandlerBase : IDocumentChangedEventHandler
    {
        public virtual Task<IEnumerable<IExternalCommand>> CreatedAsync(Document document, object entity)
        {
            return Task.FromResult(Enumerable.Empty<IExternalCommand>());
        }

        public virtual Task<IEnumerable<IExternalCommand>> DeletedAsync(Document document, object entity)
        {
            return Task.FromResult(Enumerable.Empty<IExternalCommand>());
        }

        public virtual Task<IEnumerable<IExternalCommand>> UpdatedAsync(Document document, object entity)
        {
            return Task.FromResult(Enumerable.Empty<IExternalCommand>());
        }
    }
}
