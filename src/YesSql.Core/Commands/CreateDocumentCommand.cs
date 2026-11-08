using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using YesSql.Diagnostics;

namespace YesSql.Commands
{
    public sealed class CreateDocumentCommand : DocumentCommand, ICachedCommand
    {
        private readonly IStore _store;

        public override int ExecutionOrder { get; } = 0;

        public CreateDocumentCommand(Document document, IStore store, string collection) : base(document, collection)
        {
            _store = store;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger, CancellationToken cancellationToken = default)
        {
            var insertCmd = GetInsertCommandText(dialect, _store, Collection);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(insertCmd);
            }

            var operation = new DbOperation(_store.Configuration, connection, DbOperation.Insert, _store.Configuration.TableNameConvention.GetDocumentTable(Collection), insertCmd, Statistics);

            return YesSqlInstrumentation.ExecuteAsync(operation, new CommandDefinition(insertCmd, Document, transaction, null, null, CommandFlags.Buffered, cancellationToken));
        }

        Task ICachedCommand.ExecuteAsync(DbCommandCache commandCache, DbTransaction transaction, ISqlDialect dialect, ILogger logger, CancellationToken cancellationToken)
        {
            var insertCmd = GetInsertCommandText(dialect, _store, Collection);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(insertCmd);
            }

            var command = commandCache.GetCommand(insertCmd, out var created);

            if (created)
            {
                DbCommandCache.AddParameter(command, "@Id", DbType.Int64);
                DbCommandCache.AddParameter(command, "@Type", DbType.String);
                DbCommandCache.AddParameter(command, "@Content", DbType.String);
                DbCommandCache.AddParameter(command, "@Version", DbType.Int64);
            }

            var parameters = command.Parameters;
            parameters[0].Value = Document.Id;
            DbCommandCache.SetStringValue(parameters[1], Document.Type);
            DbCommandCache.SetStringValue(parameters[2], Document.Content);
            parameters[3].Value = Document.Version;

            var operation = new DbOperation(_store.Configuration, commandCache.Connection, DbOperation.Insert, _store.Configuration.TableNameConvention.GetDocumentTable(Collection), insertCmd, Statistics);

            return YesSqlInstrumentation.ExecuteAsync(operation, (command, cancellationToken), static state => state.command.ExecuteNonQueryAsync(state.cancellationToken));
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

            return true;
        }
    }
}
