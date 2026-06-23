using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
namespace YesSql.Commands
{
    public class DeleteDocumentCommand : DocumentCommand
    {
        private readonly IStore _store;
        private readonly ISession _session;
        private readonly object _entity;
        public override int ExecutionOrder { get; } = 4;

        public DeleteDocumentCommand(object entity, Document document, IStore store, string collection, ISession session) : base(document, collection)
        {
            _store = store;
            _session = session;
            _entity = entity;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger, CancellationToken cancellationToken = default)
        {
            await _session.DocumentCommandHandler.RemovingAsync(CreateContext(_session, _entity, _store, connection, transaction, dialect));

            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);
            var deleteCmd = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable, _store.Configuration.Schema)} where {dialect.QuoteForColumnName("Id")} = @Id;";

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(deleteCmd);
            }
            await connection.ExecuteAsync(new CommandDefinition(deleteCmd, Document, transaction, null, null, CommandFlags.Buffered, cancellationToken));
        }

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand command, List<Action<DbDataReader>> actions, int index)
        {
            _session.DocumentCommandHandler.RemovingInBatch(CreateBatchContext(_session, _entity, command, queries));

            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);

            var deleteCmd = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable, _store.Configuration.Schema)} where {dialect.QuoteForColumnName("Id")} = @Id_{index};";
            queries.Add(deleteCmd);
            command.AddParameter($"Id_{index}", Document.Id);

            return true;
        }
    }
}
