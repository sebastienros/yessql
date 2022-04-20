using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public sealed class DeleteDocumentCommand : DocumentCommand
    {
        private readonly IStore _store;
        public override int ExecutionOrder { get; } = 4;

        public DeleteDocumentCommand(Document document, IStore store, string collection) : base(document, collection)
        {
            _store = store;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);
            var deleteCmd = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable)} where {dialect.QuoteForColumnName("Id")} = @Id;";
            
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(deleteCmd);
            }
            
            return connection.ExecuteAsync(deleteCmd, Document, transaction);
        }

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand command, List<Action<DbDataReader>> actions, int index)
        {
            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);

            var deleteCmd = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable)} where {dialect.QuoteForColumnName("Id")} = @Id_{index};";
            queries.Add(deleteCmd);
            command.AddParameter($"Id_{index}", Document.Id, DbType.Int64);

            return true;
        }
    }
}
