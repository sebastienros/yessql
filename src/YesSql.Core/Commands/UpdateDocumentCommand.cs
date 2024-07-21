using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Commands.DocumentChanged;

namespace YesSql.Commands
{
    public class UpdateDocumentCommand : DocumentCommand
    {
        private readonly IStore _store;
        private readonly ISession _session;
        private readonly object _entity;
        private readonly long _checkVersion;
        public override int ExecutionOrder { get; } = 2;

        public UpdateDocumentCommand(object entity, Document document, IStore store, long checkVersion, string collection, Session session) : base(document, collection)
        {
            _store = store;
            _checkVersion = checkVersion;
            _session = session;
            _entity = entity;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);

            var updateCmd = $"update {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable, _store.Configuration.Schema)} "
                + $"set {dialect.QuoteForColumnName("Content")} = @Content, {dialect.QuoteForColumnName("Version")} = @Version where "
                + $"{dialect.QuoteForColumnName("Id")} = @Id "
                + (_checkVersion > -1
                    ? Document.Version == 1 // When the Document.Version is 0 + 1 the Version column maybe null.
                        ? $" and ({dialect.QuoteForColumnName("Version")} IS NULL OR {dialect.QuoteForColumnName("Version")} = {dialect.GetSqlValue(_checkVersion)}) ;"
                        : $" and {dialect.QuoteForColumnName("Version")} = {dialect.GetSqlValue(_checkVersion)} ;"
                    : ";")
                ;

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(updateCmd);
            }

            var updatedCount = await connection.ExecuteAsync(updateCmd, Document, transaction);

            if (_checkVersion > -1 && updatedCount != 1)
            {
                throw new ConcurrencyException();
            }

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
            await _session.DocumentCommandHandler.UpdatedAsync(context);
        }

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand batchCommand, List<Action<DbDataReader>> actions, int index)
        {
            // Can't batch if version needs to be checked
            // TODO: If the scalar result still works in batches, we might need to count what is the expected total number
            // and compare this value. However deletes are a "best effort" so they might delete 0 or many items, and the updates might
            // need their own batch commands.

            if (_checkVersion > -1)
            {
                return false;
            }

            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);

            var updateCmd = $"update {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable, _store.Configuration.Schema)} "
                + $"set {dialect.QuoteForColumnName("Content")} = @Content_{index}, {dialect.QuoteForColumnName("Version")} = @Version_{index} where "
                + $"{dialect.QuoteForColumnName("Id")} = @Id_{index};"
                ;

            queries.Add(updateCmd);

            batchCommand
                .AddParameter("Id_" + index, Document.Id)
                .AddParameter("Content_" + index, Document.Content)
                .AddParameter("Version_" + index, Document.Version);

            var context = new DocumentChangeInBatchContext
            {
                Session = _session,
                Entity = _entity,
                Document = Document,
                BatchCommand = batchCommand,
                Queries = queries,
            };
            _session.DocumentCommandHandler.UpdatedInBatch(context);

            return true;
        }
    }
}
