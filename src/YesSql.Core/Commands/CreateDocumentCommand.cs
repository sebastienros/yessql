using Dapper;
using Microsoft.Extensions.Logging;
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

        public CreateDocumentCommand(IEnumerable<Document> documents, ITableNameConvention tableNameConvention, string tablePrefix, string collection) : base(documents, collection)
        {
            _tableNameConvention = tableNameConvention;
            _tablePrefix = tablePrefix;
        }

        public CreateDocumentCommand(Document document, ITableNameConvention tableNameConvention, string tablePrefix, string collection) : base(document, collection)
        {
            _tableNameConvention = tableNameConvention;
            _tablePrefix = tablePrefix;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = _tableNameConvention.GetDocumentTable(Collection);

            var insertCmd = "insert into " + dialect.QuoteForTableName(_tablePrefix + documentTable) + " (" + dialect.QuoteForColumnName("Id") + ", " + dialect.QuoteForColumnName("Type") + ", " + dialect.QuoteForColumnName("Content") + ", " + dialect.QuoteForColumnName("Version") + ") values (@Id, @Type, @Content, @Version);";

            logger.LogTrace(insertCmd);

            return connection.ExecuteAsync(insertCmd, Documents, transaction);
        }
    }
}
