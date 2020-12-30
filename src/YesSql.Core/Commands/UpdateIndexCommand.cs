using Dapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql.Commands
{
    public sealed class UpdateIndexCommand : IndexCommand
    {
        private readonly IEnumerable<int> _addedDocumentIds;
        private readonly IEnumerable<int> _deletedDocumentIds;

        public override int ExecutionOrder { get; } = 3;

        public UpdateIndexCommand(
            IIndex index,
            IEnumerable<int> addedDocumentIds,
            IEnumerable<int> deletedDocumentIds,
            IStore _store,
            string collection) : base(index, _store, collection)
        {
            _addedDocumentIds = addedDocumentIds;
            _deletedDocumentIds = deletedDocumentIds;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var type = Index.GetType();

            var sql = Updates(type, dialect);
            logger.LogTrace(sql);
            await connection.ExecuteAsync(sql, Index, transaction);

            // Update the documents list
            if (Index is ReduceIndex reduceIndex)
            {
                var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);
                var bridgeTableName = _store.Configuration.TableNameConvention.GetIndexTable(type, Collection) + "_" + documentTable;
                var columnList = dialect.QuoteForTableName(type.Name + "Id") + ", " + dialect.QuoteForColumnName("DocumentId");
                var bridgeSqlAdd = "insert into " + dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName) + " (" + columnList + ") values (@Id, @DocumentId);";
                var bridgeSqlRemove = "delete from " + dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName) + " where " + dialect.QuoteForColumnName("DocumentId") + " = @DocumentId and " + dialect.QuoteForColumnName(type.Name + "Id") + " = @Id;";

                if (_addedDocumentIds.Any())
                {
                    var dynamicParamsAdded = new DynamicParameters();
                    foreach (var id in _addedDocumentIds)
                    {
                        dynamicParamsAdded.AddDynamicParams(new { DocumentId = id, Id = Index.Id });
                    }

                    logger.LogTrace(bridgeSqlAdd);
                    await connection.ExecuteAsync(bridgeSqlAdd, dynamicParamsAdded, transaction);
                }

                if (_deletedDocumentIds.Any())
                {
                    var dynamicParamsDeleted = new DynamicParameters();
                    foreach (var id in _deletedDocumentIds)
                    {
                        dynamicParamsDeleted.AddDynamicParams(new { DocumentId = id, Id = Index.Id });
                    }

                    logger.LogTrace(bridgeSqlRemove);
                    await connection.ExecuteAsync(bridgeSqlRemove, dynamicParamsDeleted, transaction);
                }
            }
        }

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, Dictionary<string, object> parameters)
        {
            return false;
        }
    }
}
