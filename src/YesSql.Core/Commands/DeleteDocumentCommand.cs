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

        public override int ExecutionOrder { get; } = 4;

        public DeleteDocumentCommand(Document document, string tablePrefix) : base(document)
        {
            _tablePrefix = tablePrefix;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var dialect = SqlDialectFactory.For(connection);
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var deleteCmd = $"delete from [{_tablePrefix}{documentTable}] where [Id] = @Id;";
            return connection.ExecuteAsync(deleteCmd, Document, transaction);
        }
    }
}
