using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Collections;

namespace YesSql.Commands
{
    public sealed class CreateDocumentCommand : DocumentCommand
    {
        private readonly string _tablePrefix;

        public override int ExecutionOrder { get; } = 0;

        public CreateDocumentCommand(string collectionSafeName, IEnumerable<Document> documents, string tablePrefix) : base(collectionSafeName, documents)
        {
            _tablePrefix = tablePrefix;
        }

        public CreateDocumentCommand(string collectionName, Document document, string tablePrefix) : base(collectionName, document)
        {
            _tablePrefix = tablePrefix;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = CollectionHelper.GetPrefixedName(CollectionName, Store.DocumentTable);
            var insertCmd = "insert into " + dialect.QuoteForTableName(_tablePrefix + documentTable) + " (" + dialect.QuoteForColumnName("Id") + ", " + dialect.QuoteForColumnName("Type") + ", " + dialect.QuoteForColumnName("Content") + ", " + dialect.QuoteForColumnName("Version") + ") values (@Id, @Type, @Content, @Version);";

            logger.LogTrace(insertCmd);

            return connection.ExecuteAsync(insertCmd, Documents, transaction);
        }
    }
}
