using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public sealed class DeleteMapIndexCommand : IIndexCommand, ICollectionName
    {
        public IEnumerable<int> DocumentIds { get; }
        public Type IndexType { get; }
        private readonly string _tablePrefix;

        public string Collection { get; }
        public int ExecutionOrder { get; } = 1;

        public DeleteMapIndexCommand(Type indexType, IEnumerable<int> documentIds, string tablePrefix, string collection)
        {
            IndexType = indexType;
            DocumentIds = documentIds;
            _tablePrefix = tablePrefix;
            Collection = collection;
        }

        public Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger )
        {
            var command = "delete from " + dialect.QuoteForTableName(_tablePrefix + Store.GetIndexTable(IndexType, Collection)) + " where " + dialect.QuoteForColumnName("DocumentId") + " = @Id";
            logger.LogTrace(command);
            return connection.ExecuteAsync(command, DocumentIds.Select(x => new { Id = x }), transaction);
        }
    }
}
