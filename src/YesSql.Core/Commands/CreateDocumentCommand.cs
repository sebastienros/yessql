using Dapper;
using System.Data;
using System.Threading.Tasks;
using YesSql.Collections;

namespace YesSql.Commands
{
    public class CreateDocumentCommand : DocumentCommand
    {
        private string _tablePrefix;

        public override int ExecutionOrder { get; } = 0;

        public CreateDocumentCommand(Document document, string tablePrefix) : base(document)
        {
            _tablePrefix = tablePrefix;
        }

        public override Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction, ISqlDialect dialect)
        {
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var insertCmd = "insert into " + dialect.QuoteForTableName(_tablePrefix + documentTable) + " (" + dialect.QuoteForColumnName("Id") + ", " + dialect.QuoteForColumnName("Type") + ", " + dialect.QuoteForColumnName("Content") + ") values (@Id, @Type, @Content);";
            return connection.ExecuteScalarAsync<int>(insertCmd, Document, transaction);
        }
    }
}
