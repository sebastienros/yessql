using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public sealed class DeleteMapIndexCommand : IIndexCommand, ICollectionName
    {
        private readonly IStore _store;
        public long DocumentId { get; }
        public Type IndexType { get; }
        public string Collection { get; }
        public int ExecutionOrder { get; } = 1;

        public DeleteMapIndexCommand(Type indexType, long documentId, IStore store, string collection)
        {
            IndexType = indexType;
            DocumentId = documentId;
            Collection = collection;
            _store = store;
        }

        public Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger, CancellationToken cancellationToken = default)
        {
            var command = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(IndexType, Collection), _store.Configuration.Schema)} where {dialect.QuoteForColumnName("DocumentId")} = @Id;";
            
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(command);
            }

            return connection.ExecuteAsync(new CommandDefinition(command, new { Id = DocumentId }, transaction, null, null, CommandFlags.Buffered, cancellationToken));
        }

        public bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand command, List<Action<DbDataReader>> actions, int index)
        {
            var sql = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(IndexType, Collection), _store.Configuration.Schema)} where {dialect.QuoteForColumnName("DocumentId")} = @Id_{index};";

            queries.Add(sql);

            command.AddParameter($"Id_{index}", DocumentId);

            return true;
        }
    }
}
