using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql.Commands
{
    public sealed class DeleteReduceIndexCommand : IndexCommand
    {
        public DeleteReduceIndexCommand(IIndex index, IStore store, string collection) : base(index, store, collection)
        {
        }

        public override int ExecutionOrder { get; } = 1;

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand batchCommand, List<Action<DbDataReader>> actions, int index)
        {
            var type = Index.GetType();
            var name = type.Name;

            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);
            var bridgeTableName = _store.Configuration.TableNameConvention.GetIndexTable(type, Collection) + "_" + documentTable;
            var bridgeSql = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName, _store.Configuration.Schema)} where {dialect.QuoteForColumnName(name + "Id")} = @Id_{index};";
            var command = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection), _store.Configuration.Schema)} where { dialect.QuoteForColumnName("Id")} = @Id_{index};";
            queries.Add(bridgeSql);
            queries.Add(command);
            batchCommand.AddParameter("Id_" + index, Index.Id);

            return true;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger, CancellationToken cancellationToken)
        {
            var type = Index.GetType();
            var name = type.Name;

            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);
            var bridgeTableName = _store.Configuration.TableNameConvention.GetIndexTable(type, Collection) + "_" + documentTable;
            var bridgeSql = "delete from " + dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName, _store.Configuration.Schema) +" where " + dialect.QuoteForColumnName(name + "Id") + " = @Id;";
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(bridgeSql);
            }
            await connection.ExecuteAsync(new CommandDefinition(bridgeSql, new { Id = Index.Id }, transaction, null, null, CommandFlags.Buffered, cancellationToken));
            var command = "delete from " + dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection), _store.Configuration.Schema) + " where " + dialect.QuoteForColumnName("Id") + " = @Id;";
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(command);
            }
            await connection.ExecuteAsync(command, new { Id = Index.Id }, transaction);
        }
    }
}
