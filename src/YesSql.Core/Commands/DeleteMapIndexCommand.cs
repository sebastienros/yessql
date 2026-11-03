using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using YesSql.Diagnostics;

namespace YesSql.Commands
{
    public sealed class DeleteMapIndexCommand : IIndexCommand, ICollectionName, IInstrumentedCommand
    {
        private readonly IStore _store;
        public long DocumentId { get; }
        public Type IndexType { get; }
        public string Collection { get; }
        public int ExecutionOrder { get; } = 1;

        YesSqlStatistics IInstrumentedCommand.Statistics { set => _statistics = value; }

        private YesSqlStatistics _statistics;

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

            var operation = new DbOperation(_store.Configuration, connection, DbOperation.Delete, _store.Configuration.TableNameConvention.GetIndexTable(IndexType, Collection), command, _statistics);

            return YesSqlInstrumentation.ExecuteAsync(operation, new CommandDefinition(command, new { Id = DocumentId }, transaction, null, null, CommandFlags.Buffered, cancellationToken));
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
