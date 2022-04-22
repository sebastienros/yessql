using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;

namespace YesSql.Commands
{
    public sealed class CreateDocumentCommand : DocumentCommand
    {
        private readonly ITableNameConvention _tableNameConvention;
        private readonly string _tablePrefix;

        public override int ExecutionOrder { get; } = 0;

        public CreateDocumentCommand(Document document, ITableNameConvention tableNameConvention, string tablePrefix, string collection) : base(document, collection)
        {
            _tableNameConvention = tableNameConvention;
            _tablePrefix = tablePrefix;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = _tableNameConvention.GetDocumentTable(Collection);

            var tableName = _tablePrefix + documentTable;
            var insertCmd = "insert into " + dialect.QuoteForTableName(tableName) + " (" + dialect.QuoteForColumnName("Id") + ", " + dialect.QuoteForColumnName("Type") + ", " + dialect.QuoteForColumnName("Content") + ", " + dialect.QuoteForColumnName("Version") + ") " +
                                  "values (" + dialect.QuoteForParameter("Id") + ", " + dialect.QuoteForParameter("Type") + ", " + dialect.QuoteForParameter("Content") + ", " + dialect.QuoteForParameter("Version") + ")" + dialect.StatementEnd;

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(insertCmd);
            }

            var parameters = dialect.GetDynamicParameters(connection, Document, tableName);
            return connection.ExecuteScalarAsync<int>(insertCmd, parameters, transaction);
        }

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand batchCommand, List<Action<DbDataReader>> actions, int index)
        {
            var documentTable = _tableNameConvention.GetDocumentTable(Collection);
            var tableName = _tablePrefix + documentTable;
            var insertCmd = $"insert into {dialect.QuoteForTableName(tableName)} ({dialect.QuoteForColumnName("Id")}, " +
                            $"{dialect.QuoteForColumnName("Type")}, {dialect.QuoteForColumnName("Content")}, {dialect.QuoteForColumnName("Version")}) " +
                            $"values ({dialect.QuoteForParameter("Id")}_{index}, {dialect.QuoteForParameter("Type")}_{index}, {dialect.QuoteForParameter("Content")}_{index}, {dialect.QuoteForParameter("Version")}_{index})" + dialect.BatchStatementEnd;

            queries.Add(insertCmd);

            batchCommand
                .AddParameter("Id_" + index, Document.Id)
                .AddParameter("Type_" + index, Document.Type)
                .AddParameter("Content_" + index, Document.Content)
                .AddParameter(dialect.GetParameterName("Version") + "_" + index, Document.Version);

            return true;
        }
    }
}
