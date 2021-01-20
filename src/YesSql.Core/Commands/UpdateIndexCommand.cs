using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Indexes;
using YesSql.Sql.Schema;

namespace YesSql.Commands
{
    public sealed class UpdateIndexCommand : IndexCommand
    {
        private readonly int[] _addedDocumentIds;
        private readonly int[] _deletedDocumentIds;

        public override int ExecutionOrder { get; } = 3;

        public UpdateIndexCommand(
            IIndex index,
            IEnumerable<int> addedDocumentIds,
            IEnumerable<int> deletedDocumentIds,
            IStore _store,
            string collection) : base(index, _store, collection)
        {
            _addedDocumentIds = addedDocumentIds.ToArray();
            _deletedDocumentIds = deletedDocumentIds.ToArray();
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var type = Index.GetType();

            var sql = Updates(type, dialect);
            sql = sql.Replace(ParameterSuffix, "");
            logger.LogTrace(sql);
            await connection.ExecuteAsync(sql, Index, transaction);

            // Update the documents list
            if (Index is ReduceIndex)
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

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DynamicParameters parameters, List<Action<DbDataReader>> actions)
        {
            var type = Index.GetType();
            var index = queries.Count;
            var sql = Updates(type, dialect);
            sql = sql.Replace(ParameterSuffix, index.ToString());
            queries.Add(sql);

            GetProperties(parameters, Index, index.ToString(), dialect);

            parameters.Add($"Id{index}", Index.Id, System.Data.DbType.Int32);

            // Update the documents list
            if (Index is ReduceIndex)
            {
                var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);
                var bridgeTableName = _store.Configuration.TableNameConvention.GetIndexTable(type, Collection) + "_" + documentTable;
                var columnList = dialect.QuoteForTableName(type.Name + "Id") + ", " + dialect.QuoteForColumnName("DocumentId");

                parameters.Add($"Id_{index}", Index.Id);

                for (var i = 0; i < _addedDocumentIds.Length; i++)
                {
                    var bridgeSqlAdd = $"insert into {dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName)} ({columnList}) values (@Id_{index}, @AddedId_{index}_{i});";
                    queries.Add(bridgeSqlAdd);
                    parameters.Add($"AddedId_{index}_{i}", _addedDocumentIds[i]);
                }

                for (var i = 0; i < _deletedDocumentIds.Length; i++)
                {
                    var bridgeSqlRemove = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName)} where {dialect.QuoteForColumnName("DocumentId")} = @RemovedId_{index}_{i} and {dialect.QuoteForColumnName(type.Name + "Id")} = @Id_{index};";
                    queries.Add(bridgeSqlRemove);
                    parameters.Add($"RemovedId_{index}_{i}", _deletedDocumentIds[i]);
                }
            }

            return true;
        }
    }
}
