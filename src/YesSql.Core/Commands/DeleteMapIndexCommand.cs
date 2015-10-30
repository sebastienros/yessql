using Dapper;
using System;
using System.Data;
using System.Threading.Tasks;

namespace YesSql.Core.Commands
{
    public class DeleteMapIndexCommand : IIndexCommand
    {
        private readonly int _documentId;
        private readonly Type _indexType;

        public DeleteMapIndexCommand(Type indexType, int documentId)
        {
            _indexType = indexType;
            _documentId = documentId;
        }

        public virtual async Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction)
        {
            await connection.ExecuteAsync($"delete from {_indexType.Name} where DocumentId = @Id", new { Id = _documentId }, transaction);
        }
    }
}
