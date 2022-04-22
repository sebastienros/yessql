using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;

namespace YesSql.Commands
{
    public sealed class UpdateDocumentCommand : DocumentCommand
    {
        private readonly IStore _store;
        private readonly long _checkVersion;

        public override int ExecutionOrder { get; } = 2;

        public UpdateDocumentCommand(Document document, IStore store, long checkVersion, string collection) : base(document, collection)
        {
            _store = store;
            _checkVersion = checkVersion;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);

            var updateCmd = $"update {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable)} "
                + $"set {dialect.QuoteForColumnName("Content")} = {dialect.QuoteForParameter("Content")}, " +
                $"{dialect.QuoteForColumnName("Version")} = {dialect.QuoteForParameter("Version")} where "
                + $"{dialect.QuoteForColumnName("Id")} = {dialect.QuoteForParameter("Id")} "
                + (_checkVersion > -1
                    ? Document.Version == 1 // When the Document.Version is 0 + 1 the Version column maybe null.
                        ? $" and ({dialect.QuoteForColumnName("Version")} IS NULL OR {dialect.QuoteForColumnName("Version")} = {dialect.GetSqlValue(_checkVersion)}){dialect.StatementEnd}"
                        : $" and {dialect.QuoteForColumnName("Version")} = {dialect.GetSqlValue(_checkVersion)}{dialect.StatementEnd}"
                    : dialect.StatementEnd);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(updateCmd);
            }

            var parameters = dialect.GetDynamicParameters(connection, Document, _store.Configuration.TablePrefix + documentTable);
            var updatedCount = await connection.ExecuteAsync(updateCmd, parameters, transaction);

            if (_checkVersion > -1 && updatedCount != 1)
            {
                throw new ConcurrencyException();
            }
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

            var updateCmd = $"update {dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable)} "
                + $"set {dialect.QuoteForColumnName("Content")} = {dialect.QuoteForParameter("Content")}_{index}," +
                $" {dialect.QuoteForColumnName("Version")} = {dialect.QuoteForParameter("Version")}_{index} where "
                + $"{dialect.QuoteForColumnName("Id")} = {dialect.QuoteForParameter("Id")}_{index}{dialect.BatchStatementEnd}";

            queries.Add(updateCmd);

            batchCommand
                .AddParameter("Id_" + index, Document.Id, DbType.Int32)
                .AddParameter("Content_" + index, Document.Content, DbType.String)
                .AddParameter(dialect.GetParameterName("Version") + "_" + index, Document.Version, DbType.Int64);

            return true;
        }
    }
}
