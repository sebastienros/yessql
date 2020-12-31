using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

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

            var insertCmd = $"insert into {dialect.QuoteForTableName(_tablePrefix + documentTable)} ({dialect.QuoteForColumnName("Id")}, {dialect.QuoteForColumnName("Type")}, {dialect.QuoteForColumnName("Content")}, {dialect.QuoteForColumnName("Version")}) values (@Id, @Type, @Content, @Version);";

            logger.LogTrace(insertCmd);

            return connection.ExecuteAsync(insertCmd, Document, transaction);
        }

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DynamicParameters parameters, List<Action<DbDataReader>> actions)
        {
            var index = queries.Count;

            var documentTable = _tableNameConvention.GetDocumentTable(Collection);
            var insertCmd = $"insert into {dialect.QuoteForTableName(_tablePrefix + documentTable)} ({dialect.QuoteForColumnName("Id")}, {dialect.QuoteForColumnName("Type")}, {dialect.QuoteForColumnName("Content")}, {dialect.QuoteForColumnName("Version")}) values (@Id_{index}, @Type_{index}, @Content_{index}, @Version_{index});";

            queries.Add(insertCmd);
            
            parameters.Add("Id_" + index, Document.Id);
            parameters.Add("Type_" + index, Document.Type);
            parameters.Add("Content_" + index, Document.Content);
            parameters.Add("Version_" + index, Document.Version);

            return true;
        }
    }
}
