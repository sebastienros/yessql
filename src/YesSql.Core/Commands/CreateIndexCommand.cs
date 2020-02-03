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
            string tablePrefix) : base(index, tablePrefix)
        {
            _addedDocumentIds = addedDocumentIds;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var type = Index.GetType();
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var insertSql = Inserts(type, dialect);
            logger.LogTrace(insertSql);
            Index.Id = dialect.InsertReturningIndexId(connection, Index, insertSql, transaction);

            if (Index is MapIndex)
            {
                var command = "update " + dialect.QuoteForTableName(_tablePrefix + type.Name) + " set " + dialect.QuoteForColumnName("DocumentId") + " =" + dialect.QuoteForParameter("mapid") + " where " + dialect.QuoteForColumnName("Id") + " = " + dialect.QuoteForParameter("Id");
                logger.LogTrace(command);
                await connection.ExecuteAsync(command, new { mapid = Index.GetAddedDocuments().Single().Id, Id = Index.Id }, transaction);
            }
            else
            {
                var bridgeTableName = type.Name + "_" + documentTable;
                var columnList = dialect.QuoteForColumnName(type.Name + "Id") +", " + dialect.QuoteForColumnName("DocumentId");
                var bridgeSql = "insert into " + dialect.QuoteForTableName(_tablePrefix + bridgeTableName) + " (" + columnList + ") values (" + dialect.QuoteForParameter("Id") + ", " + dialect.QuoteForParameter("DocumentId") + ")" + dialect.StatementEnd;
                logger.LogTrace(bridgeSql);
                await connection.ExecuteAsync(bridgeSql, _addedDocumentIds.Select(x => new { DocumentId = x, Id = Index.Id }), transaction);
            }
        }
    }
}
