using Dapper;
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Sql;

namespace YesSql.Commands
{
    public class DeleteMapIndexCommand : IIndexCommand
    {
        private readonly int _documentId;
        private readonly Type _indexType;
        private readonly string _tablePrefix;

        public int ExecutionOrder { get; } = 1;

        public DeleteMapIndexCommand(Type indexType, int documentId, string tablePrefix, ISqlDialect dialect)
        {
            _indexType = indexType;
            _documentId = documentId;
            _tablePrefix = tablePrefix;
        }

        public virtual Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect)
        {
            return connection.ExecuteAsync("delete from " + dialect.QuoteForTableName(_tablePrefix + _indexType.Name) + " where " + dialect.QuoteForColumnName("DocumentId") + " = @Id", new { Id = _documentId }, transaction);
        }
    }
}
