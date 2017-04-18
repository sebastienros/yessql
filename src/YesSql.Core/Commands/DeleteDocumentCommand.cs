using System.Data;
using System.Threading.Tasks;
using Dapper;
using YesSql.Collections;
using YesSql.Indexes;
using YesSql.Services;
using YesSql.Sql;

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

        public override Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction, ISqlDialect dialect)
        {
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var deleteCmd = "delete from " + dialect.QuoteForTableName(_tablePrefix + documentTable) + " where " + dialect.QuoteForColumnName("Id") + " = @Id;";
            return connection.ExecuteAsync(deleteCmd, Document, transaction);
        }
    }
}
