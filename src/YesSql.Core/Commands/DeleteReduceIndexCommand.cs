using Dapper;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Core.Indexes;
using YesSql.Core.Collections;
using YesSql.Core.Services;

namespace YesSql.Core.Commands
{
    public class DeleteReduceIndexCommand : IndexCommand
    {
        public DeleteReduceIndexCommand(IIndex index, string tablePrefix) : base(index, tablePrefix)
        {
        }

        public override int ExecutionOrder { get; } = 1;

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var name = Index.GetType().Name;

            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var bridgeTableName = name + "_" + documentTable;
            var bridgeSql = $"delete from [{_tablePrefix}{bridgeTableName}] where {name}Id = @Id";

            await connection.ExecuteAsync(bridgeSql, new { Id = Index.Id }, transaction);

            await connection.ExecuteAsync($"delete from [{_tablePrefix}{name}] where Id = @Id", new { Id = Index.Id }, transaction);
        }
    }
}
