using Dapper;
using System.Data;
using System.Threading.Tasks;
using YesSql.Indexes;
using YesSql.Collections;
using YesSql.Services;
using YesSql.Sql;

namespace YesSql.Commands
{
    public class DeleteReduceIndexCommand : IndexCommand
    {
        public DeleteReduceIndexCommand(IIndex index, string tablePrefix) : base(index, tablePrefix)
        {
        }

        public override int ExecutionOrder { get; } = 1;

        public override async Task ExecuteAsync(IDbConnection connection, IDbTransaction transaction, ISqlDialect dialect)
        {
            var name = Index.GetType().Name;

            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var bridgeTableName = name + "_" + documentTable;
            var bridgeSql = "delete from " + dialect.QuoteForTableName(_tablePrefix + bridgeTableName) +" where " + dialect.QuoteForColumnName(name + "Id") + " = @Id";

            await connection.ExecuteAsync(bridgeSql, new { Id = Index.Id }, transaction);

            await connection.ExecuteAsync("delete from " + dialect.QuoteForTableName(_tablePrefix + name) + " where " + dialect.QuoteForColumnName("Id") + " = @Id", new { Id = Index.Id }, transaction);
        }
    }
}
