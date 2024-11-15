using System.Threading.Tasks;
using YesSql.Commands.DocumentChanged;

namespace YesSql.Commands
{
    public class DefaultDocumentCommandHandler : IDocumentCommandHandler
    {
        public Task CreatedAsync(DocumentChangeContext context) => Task.CompletedTask;

        public bool CreatedInBatch(DocumentChangeInBatchContext context) => true;

        public Task RemovingAsync(DocumentChangeContext context) => Task.CompletedTask;

        public bool RemovingInBatch(DocumentChangeInBatchContext context) => true;

        public Task UpdatedAsync(DocumentChangeContext context) => Task.CompletedTask;

        public bool UpdatedInBatch(DocumentChangeInBatchContext context) => true;
    }
}
