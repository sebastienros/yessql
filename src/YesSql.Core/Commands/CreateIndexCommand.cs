using Dapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Indexes;

namespace YesSql.Commands
{
    public sealed class CreateIndexCommand : IndexCommand
    {
        private readonly IEnumerable<int> _addedDocumentIds;
        
        public override int ExecutionOrder { get; } = 2;
        public CreateIndexCommand(
            IIndex index,
            IEnumerable<int> addedDocumentIds,
            string tablePrefix,
            string collectionName) : base(index, tablePrefix, collectionName)
        {
            _addedDocumentIds = addedDocumentIds;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var type = Index.GetType();
            var sql = Inserts(type, dialect);
            logger.LogTrace(sql);
            Index.Id = await connection.ExecuteScalarAsync<int>(sql, Index, transaction);
            var indexTableName = _tablePrefix + CollectionHelper.GetPrefixedName(CollectionName, type.Name);

            if (Index is MapIndex)
            {
                var command = "update " + dialect.QuoteForTableName(indexTableName) + " set " + dialect.QuoteForColumnName("DocumentId") + " = @mapid where " + dialect.QuoteForColumnName("Id") + " = @Id";
                logger.LogTrace(command);
                await connection.ExecuteAsync(command, new { mapid = Index.GetAddedDocuments().Single().Id, Id = Index.Id }, transaction);
            }
            else
            {
                var reduceIndex = Index as ReduceIndex;

                var bridgeTableName = indexTableName + "_" + Store.DocumentTable;
                var columnList = dialect.QuoteForColumnName(type.Name + "Id") +", " + dialect.QuoteForColumnName("DocumentId");
                var bridgeSql = "insert into " + dialect.QuoteForTableName(bridgeTableName) + " (" + columnList + ") values (@Id, @DocumentId);";
                logger.LogTrace(bridgeSql);
                await connection.ExecuteAsync(bridgeSql, _addedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
            }
        }
    }
}
