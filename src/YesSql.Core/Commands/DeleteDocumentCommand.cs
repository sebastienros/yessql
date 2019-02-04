using Dapper;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Logging;

namespace YesSql.Commands
{
    public class DeleteDocumentCommand : DocumentCommand
    {
        private readonly string _tablePrefix;
        public override int ExecutionOrder { get; } = 4;

        public DeleteDocumentCommand(Document document, string tablePrefix) : base(document)
        {
            _tablePrefix = tablePrefix;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var deleteCmd = "delete from " + dialect.QuoteForTableName(_tablePrefix + documentTable) + " where " + dialect.QuoteForColumnName("Id") + " = @Id;";
            logger.LogSql(deleteCmd);
            return connection.ExecuteAsync(deleteCmd, Document, transaction);
        }
    }
}
