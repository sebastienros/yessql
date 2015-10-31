using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Core.Indexes;
using YesSql.Core.Sql;

namespace YesSql.Core.Commands
{
    public class UpdateIndexCommand : IndexCommand
    {
        private readonly IEnumerable<int> _addedDocumentIds;
        private readonly IEnumerable<int> _deletedDocumentIds;

        public UpdateIndexCommand(
            Index index, 
            IEnumerable<int> addedDocumentIds, 
            IEnumerable<int> deletedDocumentIds) : base(index)
        {
            _addedDocumentIds = addedDocumentIds;
            _deletedDocumentIds = deletedDocumentIds;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var dialect = SqlDialectFactory.For(connection);
            var type = Index.GetType();

            var sql = Updates(type);
            await connection.ExecuteAsync(sql, Index, transaction);

            // Update the documents list
            var reduceIndex = Index as ReduceIndex;
            if (reduceIndex != null)
            {
                var bridgeTableName = type.Name + "_Document";
                var columnList = $"[{type.Name}Id], [DocumentId]";
                var parameterList = $"@Id, @DocumentId";
                var bridgeSqlAdd = $"insert into {bridgeTableName} ({columnList}) values ({parameterList});";
                var bridgeSqlRemove = $"delete from {bridgeTableName} where DocumentId = @DocumentId and {type.Name}Id = @Id;";

                await connection.ExecuteAsync(bridgeSqlAdd, _addedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
                await connection.ExecuteAsync(bridgeSqlRemove, _deletedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
            }
        }

    }
}
