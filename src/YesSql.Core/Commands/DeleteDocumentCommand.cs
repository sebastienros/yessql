using YesSql.Core.Indexes;
using YesSql.Core.Sql;
using System.Threading.Tasks;
using Dapper;
using System.Data.Common;

namespace YesSql.Core.Commands
{
    public class DeleteDocumentCommand : DocumentCommand
    {
        private readonly string _tablePrefix;
        public DeleteDocumentCommand(Document document, string tablePrefix) : base(document)
        {
            _tablePrefix = tablePrefix;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var dialect = SqlDialectFactory.For(connection);
            var deleteCmd = $"delete from [{_tablePrefix}Document] where [Id] = @Id;";
            await connection.ExecuteAsync(deleteCmd, Document, transaction);
        }
    }
}
