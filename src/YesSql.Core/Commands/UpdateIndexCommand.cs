using Dapper;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Indexes;

namespace YesSql.Commands
{
    public class UpdateIndexCommand : IndexCommand
    {
        private readonly IEnumerable<int> _addedDocumentIds;
        private readonly IEnumerable<int> _deletedDocumentIds;

        public override int ExecutionOrder { get; } = 3;

        public UpdateIndexCommand(
            IIndex index,
            IEnumerable<int> addedDocumentIds,
            IEnumerable<int> deletedDocumentIds,
            string tablePrefix) : base(index, tablePrefix)
        {
            _addedDocumentIds = addedDocumentIds;
            _deletedDocumentIds = deletedDocumentIds;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect)
        {
            var type = Index.GetType();

            var sql = Updates(type, dialect);
            await connection.ExecuteAsync(sql, Index, transaction);

            // Update the documents list
            if (Index is ReduceIndex reduceIndex)
            {
                var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
                var bridgeTableName = type.Name + "_" + documentTable;
                var columnList = dialect.QuoteForTableName(type.Name + "Id") + ", " + dialect.QuoteForColumnName("DocumentId");
                var bridgeSqlAdd = "insert into " + dialect.QuoteForTableName(_tablePrefix + bridgeTableName) + " (" + columnList + ") values (@Id, @DocumentId);";
                var bridgeSqlRemove = "delete from " + dialect.QuoteForTableName(_tablePrefix + bridgeTableName) + " where " + dialect.QuoteForColumnName("DocumentId") + " = @DocumentId and " + dialect.QuoteForColumnName(type.Name + "Id") + " = @Id;";

                await connection.ExecuteAsync(bridgeSqlAdd, _addedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
                await connection.ExecuteAsync(bridgeSqlRemove, _deletedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
            }
        }

    }
}
