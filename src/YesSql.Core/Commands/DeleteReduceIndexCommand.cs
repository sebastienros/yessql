using Dapper;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Core.Indexes;
using YesSql.Core.Collections;
using YesSql.Core.Services;
using YesSql.Core.Sql;

namespace YesSql.Core.Commands
{
    public class DeleteReduceIndexCommand : IndexCommand
    {
        private char _openQuoteDialect;
        private char _closeQuoteDialect;

        public DeleteReduceIndexCommand(IIndex index, string tablePrefix, ISqlDialect dialect) : base(index, tablePrefix, dialect)
        {
            _openQuoteDialect = dialect.OpenQuote;
            _closeQuoteDialect = dialect.CloseQuote;
        }

        public override int ExecutionOrder { get; } = 1;

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction)
        {
            var name = Index.GetType().Name;

            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var bridgeTableName = name + "_" + documentTable;
            var bridgeSql = $"delete from {_openQuoteDialect}{_tablePrefix}{bridgeTableName}{_closeQuoteDialect} where {name}Id = @Id";

            await connection.ExecuteAsync(bridgeSql, new { Id = Index.Id }, transaction);

            await connection.ExecuteAsync($"delete from {_openQuoteDialect}{_tablePrefix}{name}{_closeQuoteDialect} where Id = @Id", new { Id = Index.Id }, transaction);
        }
    }
}
