using System.Threading.Tasks;
using YesSql.Commands.DocumentChanged;

namespace YesSql.Commands
{
    public interface IDocumentCommandHandler
    {
        Task CreatedAsync(DocumentChangeContext context);
        bool CreatedInBatch(DocumentChangeInBatchContext context);
      
        Task RemovingAsync(DocumentChangeContext context);
        bool RemovingInBatch(DocumentChangeInBatchContext context);
        Task UpdatedAsync(DocumentChangeContext context);
        bool UpdatedInBatch(DocumentChangeInBatchContext context);
    }
}
