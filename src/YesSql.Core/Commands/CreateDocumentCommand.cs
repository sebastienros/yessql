using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Commands.DocumentChanged;

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

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);

            var insertCmd = $"insert into {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable, _store.Configuration.Schema)} ({dialect.QuoteForColumnName("Id")}, {dialect.QuoteForColumnName("Type")}, {dialect.QuoteForColumnName("Content")}, {dialect.QuoteForColumnName("Version")}) values (@Id, @Type, @Content, @Version);";

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(insertCmd);
            }
            await connection.ExecuteAsync(insertCmd, Document, transaction);

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
            await _session.DocumentCommandHandler.CreatedAsync(context);
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

            var context = new DocumentChangeInBatchContext
            {
                Session = _session,
                Document = Document,
                Entity = _entity,
                BatchCommand = batchCommand,
                Queries = queries,
            };
            _session.DocumentCommandHandler.CreatedInBatch(context);

            return true;
        }
    }
}
