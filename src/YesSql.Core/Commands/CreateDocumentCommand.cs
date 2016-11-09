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

        public override int ExecutionOrder { get; } = 0;

        public CreateDocumentCommand(Document document, string tablePrefix) : base(document)
        {
            _tablePrefix = tablePrefix;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var insertCmd = $"insert into [{_tablePrefix}{documentTable}] ([Id], [Type]) values (@Id, @Type);";
            return connection.ExecuteScalarAsync<int>(insertCmd, Document, transaction);
        }
    }
}
