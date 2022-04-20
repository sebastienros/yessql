using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql.Commands
{
    public sealed class CreateIndexCommand : IndexCommand
    {
        private readonly long[] _addedDocumentIds;

        public override int ExecutionOrder { get; } = 2;

        public CreateIndexCommand(
            IIndex index,
            IEnumerable<long> addedDocumentIds,
            IStore store,
            string collection) : base(index, store, collection)
        {
            _addedDocumentIds = addedDocumentIds.ToArray();
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var type = Index.GetType();
            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);

            var sql = Inserts(type, dialect);
            sql = sql.Replace(ParameterSuffix, "");

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(sql);
            }

            if (Index is MapIndex)
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                GetProperties(command, Index, "", dialect);
                command.AddParameter($"DocumentId", Index.GetAddedDocuments().Single().Id);
                Index.Id = Convert.ToInt64(await command.ExecuteScalarAsync());
            }
            else
            {
                Index.Id = await connection.ExecuteScalarAsync<long>(sql, Index, transaction);

                var reduceIndex = Index as ReduceIndex;
                var bridgeTableName = _store.Configuration.TableNameConvention.GetIndexTable(type, Collection) + "_" + documentTable;
                var columnList = dialect.QuoteForColumnName(type.Name + "Id") + ", " + dialect.QuoteForColumnName("DocumentId");
                var bridgeSql = "insert into " + dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName) + " (" + columnList + ") values (@Id, @DocumentId);";

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace(bridgeSql);
                }

                await connection.ExecuteAsync(bridgeSql, _addedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
            }
        }

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand batchCommand, List<Action<DbDataReader>> actions, int index)
        {
            if (Index is ReduceIndex && _addedDocumentIds.Length > 1)
            {
                return false;

                // We can't batch the inserts since the last id method can't be used multiple time in a raw. Each new insert would erase the actual value we need:

                /*
                 * Example:
                 * INSERT INTO [tpArticlesByDay] ([Count], [DayOfYear]) VALUES (@Count10, @DayOfYear10) ; select last_insert_rowid() [Id];
                 * insert into [tpArticlesByDay_Document] ([ArticlesByDayId], [DocumentId]) values (last_insert_rowid(), @DocumentId_10_0);
                 * insert into [tpArticlesByDay_Document] ([ArticlesByDayId], [DocumentId]) values (last_insert_rowid(), @DocumentId_10_1);
                 * insert into [tpArticlesByDay_Document] ([ArticlesByDayId], [DocumentId]) values (last_insert_rowid(), @DocumentId_10_2);
                 * insert into [tpArticlesByDay_Document] ([ArticlesByDayId], [DocumentId]) values (last_insert_rowid(), @DocumentId_10_3);
                */

                // If there is a single reduced element however we can include it in the batch
            }

            var type = Index.GetType();
            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);
            var sql = Inserts(type, dialect);
            sql = sql.Replace(ParameterSuffix, index.ToString());
            queries.Add(sql);

            actions.Add(dr =>
            {
                dr.Read();
                Index.Id = Convert.ToInt64(dr[0]);
                dr.NextResult();
            });

            GetProperties(batchCommand, Index, index.ToString(), dialect);

            var tableName = _store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection);

            if (Index is MapIndex)
            {
                batchCommand.AddParameter($"DocumentId{index}", Index.GetAddedDocuments().Single().Id);
            }
            else
            {
                var reduceIndex = Index as ReduceIndex;
                
                var bridgeTableName = _store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection) + "_" + documentTable;
                var columnList = dialect.QuoteForColumnName(type.Name + "Id") + ", " + dialect.QuoteForColumnName("DocumentId");
                queries.Add($"insert into {dialect.QuoteForTableName(bridgeTableName)} ({columnList}) values ({dialect.IdentityLastId}, @DocumentId_{index});");
                batchCommand.AddParameter($"DocumentId_{index}", _addedDocumentIds[0]);
            }

            return true;
        }
    }
}
