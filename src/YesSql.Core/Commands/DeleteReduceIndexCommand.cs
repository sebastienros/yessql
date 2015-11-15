using Dapper;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Core.Indexes;

namespace YesSql.Core.Commands
{
    public class DeleteReduceIndexCommand : IndexCommand
    {
        public DeleteReduceIndexCommand(Index index, string tablePrefix) : base(index, tablePrefix)
        {
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var name = Index.GetType().Name;

            var bridgeTableName = name + "_Document";
            var bridgeSql = $"delete from [{_tablePrefix}{bridgeTableName}] where {name}Id = @id";

            await connection.ExecuteAsync(bridgeSql, new { Id = Index.Id }, transaction);

            await connection.ExecuteAsync($"delete from [{_tablePrefix}{name}] where Id = @Id", new { Id = Index.Id }, transaction);
        }
    }
}
