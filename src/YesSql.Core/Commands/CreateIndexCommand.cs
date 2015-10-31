using System.Data;
using YesSql.Core.Indexes;
using System.Linq;
using YesSql.Core.Sql;
using Dapper;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.Common;

namespace YesSql.Core.Commands
{
    public class CreateIndexCommand : IndexCommand
    {
        private readonly IEnumerable<int> _addedDocumentIds;

        public CreateIndexCommand(
            Index index,
            IEnumerable<int> addedDocumentIds) : base(index)
        {
            _addedDocumentIds = addedDocumentIds;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var dialect = SqlDialectFactory.For(connection);
            var type = Index.GetType();

            var mapIndex = Index as MapIndex;
            if (mapIndex != null)
            {
                var sql = Inserts(type) + $" {dialect.IdentitySelectString} id";
                Index.Id = await connection.ExecuteScalarAsync<int>(sql, Index, transaction);
                await connection.ExecuteAsync($"update {type.Name} set DocumentId = @mapid where Id = @id", new { mapid = mapIndex.GetAddedDocuments().Single().Id, id = Index.Id }, transaction);
            }
            else
            {
                var reduceIndex = Index as ReduceIndex;

                var sql = Inserts(type) + $"; {dialect.IdentitySelectString} id";
                Index.Id = await connection.ExecuteScalarAsync<int>(sql, Index, transaction);

                var bridgeTableName = type.Name + "_Document";
                var columnList = $"[{type.Name}Id], [DocumentId]";
                var parameterList = $"@Id, @DocumentId";
                var bridgeSql = $"insert into {bridgeTableName} ({columnList}) values ({parameterList});";

                await connection.ExecuteAsync(bridgeSql, _addedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
            }
        }
    }
}
