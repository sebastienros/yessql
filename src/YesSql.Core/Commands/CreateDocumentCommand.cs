using YesSql.Core.Indexes;
using YesSql.Core.Sql;
using System.Threading.Tasks;
using Dapper;
using System.Data.Common;

namespace YesSql.Core.Commands
{
    public class CreateDocumentCommand : DocumentCommand
    {
        private string _tablePrefix;

        public override int ExecutionOrder { get; } = 0;
        
        public CreateDocumentCommand(Document document, string tablePrefix) : base(document)
        {
            _tablePrefix = tablePrefix;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var dialect = SqlDialectFactory.For(connection);
            var insertCmd = $"insert into [{_tablePrefix}Document] ([Id], [Type]) values (@Id, @Type);";
            await connection.ExecuteScalarAsync<int>(insertCmd, Document, transaction);
        }
    }
}
