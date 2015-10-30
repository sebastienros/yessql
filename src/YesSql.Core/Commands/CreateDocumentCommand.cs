using System.Data;
using YesSql.Core.Indexes;
using YesSql.Core.Sql;
using System.Threading.Tasks;
using Dapper;

namespace YesSql.Core.Commands
{
    public class CreateDocumentCommand : DocumentCommand
    {
        private static string insertCmd = $"insert into Document ([Type]) values (@Type);";
        public CreateDocumentCommand(Document document) : base(document)
        {
        }

        public override async Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction)
        {
            var dialect = SqlDialectFactory.For(connection);
            Document.Id = await connection.ExecuteScalarAsync<int>(insertCmd + $"{dialect.IdentitySelectString} id;", Document, transaction);
        }
    }
}
