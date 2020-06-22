using Dapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public sealed class DeleteDocumentCommand : DocumentCommand
    {
        private readonly string _tablePrefix;
        public override int ExecutionOrder { get; } = 4;

        public DeleteDocumentCommand(IEnumerable<Document> documents, string tablePrefix, string collection) : base(documents, collection)
        {
            _tablePrefix = tablePrefix;
        }

        public DeleteDocumentCommand(Document document, string tablePrefix, string collection) : base(document, collection)
        {
            _tablePrefix = tablePrefix;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = Store.GetDocumentTable(Collection);
            var deleteCmd = "delete from " + dialect.QuoteForTableName(_tablePrefix + documentTable) + " where " + dialect.QuoteForColumnName("Id") + " = @Id;";
            logger.LogTrace(deleteCmd);
            return connection.ExecuteAsync(deleteCmd, Documents, transaction);
        }
    }
}
