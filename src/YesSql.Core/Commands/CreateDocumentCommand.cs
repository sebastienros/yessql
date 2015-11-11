using System.Data;
using YesSql.Core.Indexes;
using YesSql.Core.Sql;
using System.Threading.Tasks;
using Dapper;
using System.Data.Common;

namespace YesSql.Core.Commands
{
    public class CreateDocumentCommand : DocumentCommand
    {
        private static string insertCmd = $"insert into Document ([Id], [Type]) values (@Id, @Type);";
        public CreateDocumentCommand(Document document) : base(document)
        {
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var dialect = SqlDialectFactory.For(connection);
            await connection.ExecuteScalarAsync<int>(insertCmd, Document, transaction);
        }
    }
}
