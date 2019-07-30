using Dapper;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Collections;
using YesSql.Indexes;

namespace YesSql.Commands
{
    public sealed class DeleteReduceIndexCommand : IndexCommand
    {
        public DeleteReduceIndexCommand(IIndex index, string tablePrefix) : base(index, tablePrefix)
        {
        }

        public override int ExecutionOrder { get; } = 1;

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var name = Index.GetType().Name;

            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var bridgeTableName = name + "_" + documentTable;
            var bridgeSql = "delete from " + dialect.QuoteForTableName(_tablePrefix + bridgeTableName) +" where " + dialect.QuoteForColumnName(name + "Id") + " = @Id";
            logger.LogTrace(bridgeSql);
            await connection.ExecuteAsync(bridgeSql, new { Id = Index.Id }, transaction);
            var command = "delete from " + dialect.QuoteForTableName(_tablePrefix + name) + " where " + dialect.QuoteForColumnName("Id") + " = @Id";
            logger.LogTrace(command);
            await connection.ExecuteAsync(command, new { Id = Index.Id }, transaction);
        }
    }
}
