using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public class CreateDocumentCommand : DocumentCommand
    {
        private readonly IStore _store;
        private readonly ISession _session;
        private readonly object _entity;
        public override int ExecutionOrder { get; } = 0;

        public CreateDocumentCommand(object entity, Document document, IStore store, string collection, ISession session) : base(document, collection)
        {
            _store = store;
            _session = session;
            _entity = entity;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger, CancellationToken cancellationToken = default)
        {
            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);

            var insertCmd = $"insert into {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable, _store.Configuration.Schema)} ({dialect.QuoteForColumnName("Id")}, {dialect.QuoteForColumnName("Type")}, {dialect.QuoteForColumnName("Content")}, {dialect.QuoteForColumnName("Version")}) values (@Id, @Type, @Content, @Version);";

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(insertCmd);
            }
            await connection.ExecuteAsync(new CommandDefinition(insertCmd, Document, transaction, null, null, CommandFlags.Buffered, cancellationToken));
            await _session.DocumentCommandHandler.CreatedAsync(CreateContext(_session, _entity, _store, connection, transaction, dialect));
        }

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand batchCommand, List<Action<DbDataReader>> actions, int index)
        {
            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);
            var insertCmd = $"insert into {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable, _store.Configuration.Schema)} ({dialect.QuoteForColumnName("Id")}, {dialect.QuoteForColumnName("Type")}, {dialect.QuoteForColumnName("Content")}, {dialect.QuoteForColumnName("Version")}) values (@Id_{index}, @Type_{index}, @Content_{index}, @Version_{index});";

            queries.Add(insertCmd);

            batchCommand
                .AddParameter("Id_" + index, Document.Id)
                .AddParameter("Type_" + index, Document.Type)
                .AddParameter("Content_" + index, Document.Content)
                .AddParameter("Version_" + index, Document.Version);

            _session.DocumentCommandHandler.CreatedInBatch(CreateBatchContext(_session, _entity, batchCommand, queries));

            return true;
        }
    }
}
