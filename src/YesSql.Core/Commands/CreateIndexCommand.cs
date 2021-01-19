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
        private readonly int[] _addedDocumentIds;

        public override int ExecutionOrder { get; } = 2;

        public CreateIndexCommand(
            IIndex index,
            IEnumerable<int> addedDocumentIds,
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
            logger.LogTrace(sql);

            if (Index is MapIndex)
            {
                var parameters = new DynamicParameters();
                GetProperties(parameters, Index, "");
                parameters.Add($"DocumentId", Index.GetAddedDocuments().Single().Id, System.Data.DbType.Int32);
                Index.Id = await connection.ExecuteScalarAsync<int>(sql, parameters, transaction);
            }
            else
            {
                Index.Id = await connection.ExecuteScalarAsync<int>(sql, Index, transaction);

                var reduceIndex = Index as ReduceIndex;

                var bridgeTableName = _store.Configuration.TableNameConvention.GetIndexTable(type, Collection) + "_" + documentTable;
                var columnList = dialect.QuoteForColumnName(type.Name + "Id") + ", " + dialect.QuoteForColumnName("DocumentId");
                var bridgeSql = "insert into " + dialect.QuoteForTableName(_store.Configuration.TablePrefix + bridgeTableName) + " (" + columnList + ") values (@Id, @DocumentId);";
                logger.LogTrace(bridgeSql);
                await connection.ExecuteAsync(bridgeSql, _addedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
            }
        }

        public override bool AddToBatch(ISqlDialect dialect, List<string> queries, DynamicParameters parameters, List<Action<DbDataReader>> actions)
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
            var index = queries.Count;
            var sql = Inserts(type, dialect);
            sql = sql.Replace(ParameterSuffix, index.ToString());
            queries.Add(sql);

            actions.Add(dr =>
            {
                dr.Read();
                Index.Id = Convert.ToInt32(dr[0]);
                dr.NextResult();
            });

            GetProperties(parameters, Index, index.ToString());

            var tableName = _store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection);

            if (Index is MapIndex)
            {
                parameters.Add($"DocumentId{index}", Index.GetAddedDocuments().Single().Id);
            }
            else
            {
                var reduceIndex = Index as ReduceIndex;

                var bridgeTableName = _store.Configuration.TablePrefix + _store.Configuration.TableNameConvention.GetIndexTable(type, Collection) + "_" + documentTable;
                var columnList = dialect.QuoteForColumnName(type.Name + "Id") + ", " + dialect.QuoteForColumnName("DocumentId");
                queries.Add($"insert into {dialect.QuoteForTableName(bridgeTableName)} ({columnList}) values ({dialect.IdentityLastId}, @DocumentId_{index});");
                parameters.Add($"DocumentId_{index}", _addedDocumentIds[0]);
            }

            return true;
        }
    }
}
