using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using YesSql.Core.Collections;
using YesSql.Core.Indexes;
using YesSql.Core.Services;
using YesSql.Core.Sql;

namespace YesSql.Core.Commands
{
    public class DeleteDocumentCommand : DocumentCommand
    {
        private readonly string _tablePrefix;
        private char _openQuoteDialect;
        private char _closeQuoteDialect;
        public override int ExecutionOrder { get; } = 4;

        public DeleteDocumentCommand(Document document, string tablePrefix, ISqlDialect dialect) : base(document)
        {
            _tablePrefix = tablePrefix;
            _openQuoteDialect = dialect.OpenQuote;
            _closeQuoteDialect = dialect.CloseQuote;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var deleteCmd = $"delete from {_openQuoteDialect}{_tablePrefix}{documentTable}{_closeQuoteDialect} where {_openQuoteDialect}Id{_closeQuoteDialect} = @Id;";
            return connection.ExecuteAsync(deleteCmd, Document, transaction);
        }
    }
}
