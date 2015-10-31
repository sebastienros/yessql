using System.Data;
using YesSql.Core.Indexes;
using YesSql.Core.Sql;
using System.Threading.Tasks;
using Dapper;
using System.Data.Common;

namespace YesSql.Core.Commands
{
    public class DeleteDocumentCommand : DocumentCommand
    {
        private static string deleteCmd = $"delete from Document where [Id] = @Id;";
        public DeleteDocumentCommand(Document document) : base(document)
        {
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var dialect = SqlDialectFactory.For(connection);
            await connection.ExecuteAsync(deleteCmd, Document, transaction);
        }
    }
}
