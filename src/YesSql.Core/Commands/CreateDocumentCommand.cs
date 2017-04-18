using System.Data;
using System.Threading.Tasks;
using Dapper;
using YesSql.Collections;
using YesSql.Indexes;
using YesSql.Services;
using YesSql.Sql;

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
            var insertCmd = "insert into " + dialect.QuoteForTableName(_tablePrefix + documentTable) + " (" + dialect.QuoteForColumnName("Id") + ", " + dialect.QuoteForColumnName("Type") + ") values (@Id, @Type);";
            return connection.ExecuteScalarAsync<int>(insertCmd, Document, transaction);
        }
    }
}
