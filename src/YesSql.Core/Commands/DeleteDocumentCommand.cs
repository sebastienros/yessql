using Dapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Collections;

namespace YesSql.Commands
{
    public sealed class DeleteDocumentCommand : DocumentCommand
    {
        private readonly string _tablePrefix;
        public override int ExecutionOrder { get; } = 4;

        public DeleteDocumentCommand(string collectionName, IEnumerable<Document> documents, string tablePrefix) : base(collectionName,documents)
        {
            _tablePrefix = tablePrefix;
        }

        public DeleteDocumentCommand(string collectionSafeName, Document document, string tablePrefix) : base(collectionSafeName, document)
        {
            _tablePrefix = tablePrefix;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = CollectionHelper.GetPrefixedName(CollectionName, Store.DocumentTable);
            var deleteCmd = "delete from " + dialect.QuoteForTableName(_tablePrefix + documentTable) + " where " + dialect.QuoteForColumnName("Id") + " = @Id;";
            logger.LogTrace(deleteCmd);
            return connection.ExecuteAsync(deleteCmd, Documents, transaction);
        }
    }
}
