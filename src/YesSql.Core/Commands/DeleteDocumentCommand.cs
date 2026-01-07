using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Commands.DocumentChanged;

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

        public async override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var context = new DocumentChangeContext
            {
                Session = _session,
                Entity = _entity,
                Document = Document,
                Store = _store,
                Connection = connection,
                Transaction = transaction,
                Dialect = dialect,
            };
            await _session.DocumentCommandHandler.RemovingAsync(context);

            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);
            var deleteCmd = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable, _store.Configuration.Schema)} where {dialect.QuoteForColumnName("Id")} = @Id;";

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(deleteCmd);
            }

            await connection.ExecuteAsync(deleteCmd, Document, transaction);
        }

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand command, List<Action<DbDataReader>> actions, int index)
        {
            var context = new DocumentChangeInBatchContext
            {
                Session = _session,
                Document = Document,
                Entity = _entity,
                BatchCommand = command,
                Queries = queries,
            };
            _session.DocumentCommandHandler.RemovingInBatch(context);

            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);

            var deleteCmd = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable, _store.Configuration.Schema)} where {dialect.QuoteForColumnName("Id")} = @Id_{index};";
            queries.Add(deleteCmd);
            command.AddParameter($"Id_{index}", Document.Id);

            return true;
        }
    }
}
