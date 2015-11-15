using Dapper;
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace YesSql.Core.Commands
{
    public class DeleteMapIndexCommand : IIndexCommand
    {
        private readonly int _documentId;
        private readonly Type _indexType;
        private readonly string _tablePrefix;

        public DeleteMapIndexCommand(Type indexType, int documentId, string tablePrefix)
        {
            _indexType = indexType;
            _documentId = documentId;
            _tablePrefix = tablePrefix;
        }

        public virtual async Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            await connection.ExecuteAsync($"delete from [{_tablePrefix}{_indexType.Name}] where DocumentId = @Id", new { Id = _documentId }, transaction);
        }
    }
}
