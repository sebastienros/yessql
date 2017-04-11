using YesSql.Core.Indexes;
using System.Linq;
using YesSql.Core.Sql;
using Dapper;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.Common;
using YesSql.Core.Collections;
using YesSql.Core.Services;

namespace YesSql.Core.Commands
{
    public class CreateIndexCommand : IndexCommand
    {
        private readonly IEnumerable<int> _addedDocumentIds;

        public override int ExecutionOrder { get; } = 2;

        public CreateIndexCommand(
            IIndex index,
            IEnumerable<int> addedDocumentIds,
            string tablePrefix) : base(index, tablePrefix)
        {
            _addedDocumentIds = addedDocumentIds;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var dialect = SqlDialectFactory.For(connection);
            var type = Index.GetType();
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);

            if (Index is MapIndex)
            {
                var sql = Inserts(type) + $" {dialect.IdentitySelectString} id";
                Index.Id = await connection.ExecuteScalarAsync<int>(sql, Index, transaction);
                await connection.ExecuteAsync($"update [{_tablePrefix}{type.Name}] set DocumentId = @mapid where Id = @Id", new { mapid = Index.GetAddedDocuments().Single().Id, Id = Index.Id }, transaction);
            }
            else
            {
                var reduceIndex = Index as ReduceIndex;

                var sql = Inserts(type) + $"; {dialect.IdentitySelectString} id";
                Index.Id = await connection.ExecuteScalarAsync<int>(sql, Index, transaction);

                var bridgeTableName = type.Name + "_" + documentTable;
                var columnList = $"[{type.Name}Id], [DocumentId]";
                var parameterList = $"@Id, @DocumentId";
                var bridgeSql = $"insert into [{_tablePrefix}{bridgeTableName}] ({columnList}) values ({parameterList});";

                await connection.ExecuteAsync(bridgeSql, _addedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
            }
        }
    }
}
