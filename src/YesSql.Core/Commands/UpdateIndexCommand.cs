using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using YesSql.Indexes;

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

            var parameter = dialect.GetSafeIndexParameters(Index);
            var sql = Updates(type, dialect);
            sql = sql.Replace(ParameterSuffix, "");
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(sql);
            }
            await connection.ExecuteAsync(sql, parameter, transaction);

            // Update the documents list
            if (Index is ReduceIndex)
            {
                var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);
                var bridgeTableName = _store.Configuration.TableNameConvention.GetIndexTable(type, Collection) + "_" + documentTable;
                var columnList = dialect.QuoteForTableName(type.Name + "Id") + ", " + dialect.QuoteForColumnName("DocumentId");
                var bridgeSqlAdd = "insert into " + dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName) + " (" + columnList + ") " +
                                   "values (" + dialect.QuoteForParameter("Id") + ", " + dialect.QuoteForParameter("DocumentId") + ")" + dialect.StatementEnd;
                var bridgeSqlRemove = "delete from " + dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName) + " where " + dialect.QuoteForColumnName("DocumentId") + " =" +
                                      dialect.QuoteForParameter("DocumentId") + " and " + dialect.QuoteForColumnName(type.Name + "Id") + " = " + dialect.QuoteForParameter("Id") + dialect.StatementEnd;

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
            parameter.DbType = System.Data.DbType.Int32;
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
                parameter.DbType = System.Data.DbType.Int32;
                command.Parameters.Add(parameter);

                for (var i = 0; i < _addedDocumentIds.Length; i++)
                {
                    var bridgeSqlAdd = $"insert into {dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName)} ({columnList}) " +
                                       $"values ({dialect.QuoteForParameter("Id")}_{index}, {dialect.QuoteForParameter("AddedId")}_{index}_{i}){dialect.BatchStatementEnd}";
                    queries.Add(bridgeSqlAdd);

                    parameter = command.CreateParameter();
                    parameter.ParameterName = $"AddedId_{index}_{i}";
                    parameter.Value = _addedDocumentIds[i];
                    parameter.DbType = System.Data.DbType.Int32;
                    command.Parameters.Add(parameter);
                }

                for (var i = 0; i < _deletedDocumentIds.Length; i++)
                {
                    var bridgeSqlRemove = $"delete from {dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName)} " +
                                          $"where {dialect.QuoteForColumnName("DocumentId")} = {dialect.QuoteForParameter("RemovedId")}_{index}_{i} " +
                                          $"and {dialect.QuoteForColumnName(type.Name + "Id")} = {dialect.QuoteForParameter("Id")}_{index}{dialect.BatchStatementEnd}";
                    queries.Add(bridgeSqlRemove);

                    parameter = command.CreateParameter();
                    parameter.ParameterName = $"RemovedId_{index}_{i}";
                    parameter.Value = _deletedDocumentIds[i];
                    parameter.DbType = System.Data.DbType.Int32;
                    command.Parameters.Add(parameter);
                }
            }

            return true;
        }
    }
}
