using System.Threading.Tasks;
using YesSql.Commands.DocumentChanged;

namespace YesSql.Commands
{
    public interface IDocumentCommandHandler
    {
        Task CreatedAsync(DocumentChangeContext context);
        void CreatedInBatch(DocumentChangeInBatchContext context);
      
        Task RemovingAsync(DocumentChangeContext context);
        void RemovingInBatch(DocumentChangeInBatchContext context);
        Task UpdatedAsync(DocumentChangeContext context);
        void UpdatedInBatch(DocumentChangeInBatchContext context);
    }
}
