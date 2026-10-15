using System.Threading.Tasks;
using YesSql.Commands.DocumentChanged;

namespace YesSql.Commands
{
    public class DefaultDocumentCommandHandler : IDocumentCommandHandler
    {
        public static DefaultDocumentCommandHandler Instance { get; } = new();

        public Task CreatedAsync(DocumentChangeContext context) => Task.CompletedTask;

        public void CreatedInBatch(DocumentChangeInBatchContext context)
        {
        }

        public Task RemovingAsync(DocumentChangeContext context) => Task.CompletedTask;

        public void RemovingInBatch(DocumentChangeInBatchContext context)
        {
        }

        public Task UpdatedAsync(DocumentChangeContext context) => Task.CompletedTask;

        public void UpdatedInBatch(DocumentChangeInBatchContext context)
        {
        }
    }
}
