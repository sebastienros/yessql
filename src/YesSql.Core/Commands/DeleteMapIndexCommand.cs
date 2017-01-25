using Dapper;
using System;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Core.Sql;

namespace YesSql.Core.Commands
{
    public class DeleteMapIndexCommand : IIndexCommand
    {
        private readonly int _documentId;
        private readonly Type _indexType;
        private readonly string _tablePrefix;
        private char _openQuoteDialect;
        private char _closeQuoteDialect;

        public int ExecutionOrder { get; } = 1;

        public DeleteMapIndexCommand(Type indexType, int documentId, string tablePrefix, ISqlDialect dialect)
        {
            _indexType = indexType;
            _documentId = documentId;
            _tablePrefix = tablePrefix;
            _openQuoteDialect = dialect.OpenQuote;
            _closeQuoteDialect = dialect.CloseQuote;
        }

        public virtual Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            return connection.ExecuteAsync($"delete from {_openQuoteDialect}{_tablePrefix}{_indexType.Name}{_closeQuoteDialect} where DocumentId = @Id", new { Id = _documentId }, transaction);
        }
    }
}
