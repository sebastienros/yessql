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
        private readonly long[] _addedDocumentIds;
        private readonly long[] _deletedDocumentIds;

        public override int ExecutionOrder { get; } = 3;

        public UpdateIndexCommand(
            IIndex index,
            IEnumerable<long> addedDocumentIds,
            IEnumerable<long> deletedDocumentIds,
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
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(sql);
            }
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

                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        logger.LogTrace(bridgeSqlAdd);
                    }
                    await connection.ExecuteAsync(bridgeSqlAdd, dynamicParamsAdded, transaction);
                }

                if (_deletedDocumentIds.Any())
                {
                    var dynamicParamsDeleted = new DynamicParameters();
                    foreach (var id in _deletedDocumentIds)
                    {
                        dynamicParamsDeleted.AddDynamicParams(new { DocumentId = id, Id = Index.Id });
                    }

                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        logger.LogTrace(bridgeSqlRemove);
                    }
                    await connection.ExecuteAsync(bridgeSqlRemove, dynamicParamsDeleted, transaction);
                }
            }
        }

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand command, List<Action<DbDataReader>> actions, int index)
        {
            var type = Index.GetType();
            var sql = Updates(type, dialect);
            sql = sql.Replace(ParameterSuffix, index.ToString());
            queries.Add(sql);

            GetProperties(command, Index, index.ToString(), dialect);
            
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"Id{index}";
            parameter.Value = Index.Id;
            parameter.DbType = System.Data.DbType.Int64;
            command.Parameters.Add(parameter);

            // Update the documents list
            if (Index is ReduceIndex)
            {
                var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);
                var bridgeTableName = _store.Configuration.TableNameConvention.GetIndexTable(type, Collection) + "_" + documentTable;
                var columnList = dialect.QuoteForTableName(type.Name + "Id") + ", " + dialect.QuoteForColumnName("DocumentId");

                parameter = command.CreateParameter();
                parameter.ParameterName = $"Id_{index}";
                parameter.Value = Index.Id;
                parameter.DbType = System.Data.DbType.Int64;
                command.Parameters.Add(parameter);

                for (var i = 0; i < _addedDocumentIds.Length; i++)
                {
                    var bridgeSqlAdd = $"insert into {dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName)} ({columnList}) values (@Id_{index}, @AddedId_{index}_{i});";
                    queries.Add(bridgeSqlAdd);

                    parameter = command.CreateParameter();
                    parameter.ParameterName = $"AddedId_{index}_{i}";
                    parameter.Value = _addedDocumentIds[i];
                    parameter.DbType = System.Data.DbType.Int64;
                    command.Parameters.Add(parameter);
                }

                for (var i = 0; i < _deletedDocumentIds.Length; i++)
                {
                    var bridgeSqlRemove = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName)} where {dialect.QuoteForColumnName("DocumentId")} = @RemovedId_{index}_{i} and {dialect.QuoteForColumnName(type.Name + "Id")} = @Id_{index};";
                    queries.Add(bridgeSqlRemove);

                    parameter = command.CreateParameter();
                    parameter.ParameterName = $"RemovedId_{index}_{i}";
                    parameter.Value = _deletedDocumentIds[i];
                    parameter.DbType = System.Data.DbType.Int64;
                    command.Parameters.Add(parameter);
                }
            }

            return true;
        }
    }
}
