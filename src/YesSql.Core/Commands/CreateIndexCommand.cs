using Dapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
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
            IStore store,
            string collection) : base(index, store, collection)
        {
            _addedDocumentIds = addedDocumentIds;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var type = Index.GetType();
            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);

            var sql = Inserts(type, dialect);
            logger.LogTrace(sql);
            Index.Id = await connection.ExecuteScalarAsync<int>(sql, Index, transaction);


            if (Index is MapIndex)
            {
                var command = "update " + dialect.QuoteForTableName(_store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection)) + " set " + dialect.QuoteForColumnName("DocumentId") + " = @mapid where " + dialect.QuoteForColumnName("Id") + " = @Id";
                logger.LogTrace(command);
                await connection.ExecuteAsync(command, new { mapid = Index.GetAddedDocuments().Single().Id, Id = Index.Id }, transaction);
            }
            else
            {
                var reduceIndex = Index as ReduceIndex;

                var bridgeTableName = _store.Configuration.TableNameConvention.GetIndexTable(type, Collection) + "_" + documentTable;
                var columnList = dialect.QuoteForColumnName(type.Name + "Id") +", " + dialect.QuoteForColumnName("DocumentId");
                var bridgeSql = "insert into " + dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName) + " (" + columnList + ") values (@Id, @DocumentId);";
                logger.LogTrace(bridgeSql);
                await connection.ExecuteAsync(bridgeSql, _addedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
            }
        }
    }
}
