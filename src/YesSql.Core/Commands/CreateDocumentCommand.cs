using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using YesSql.Core.Collections;
using YesSql.Core.Indexes;
using YesSql.Core.Services;
using YesSql.Core.Sql;

namespace YesSql.Core.Commands
{
    public class CreateDocumentCommand : DocumentCommand
    {
        private string _tablePrefix;
        private char _openQuoteDialect;
        private char _closeQuoteDialect;

        public override int ExecutionOrder { get; } = 0;

        public CreateDocumentCommand(Document document, string tablePrefix, ISqlDialect dialect) : base(document)
        {
            _tablePrefix = tablePrefix;
            _openQuoteDialect = dialect.OpenQuote;
            _closeQuoteDialect = dialect.CloseQuote;

        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var insertCmd = $"insert into {_openQuoteDialect}{_tablePrefix}{documentTable}{_closeQuoteDialect} ({_openQuoteDialect}Id{_closeQuoteDialect}, {_openQuoteDialect}Type{_closeQuoteDialect}) values (@Id, @Type);";
            return connection.ExecuteScalarAsync<int>(insertCmd, Document, transaction);
        }
    }
}
