using Dapper;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Collections;

namespace YesSql.Commands
{
    public sealed class UpdateDocumentCommand : DocumentCommand
    {
        private readonly string _tablePrefix;
        private readonly long _checkVersion;

        public override int ExecutionOrder { get; } = 2;

        public UpdateDocumentCommand(Document document, string tablePrefix, long checkVersion) : base(document)
        {
            _tablePrefix = tablePrefix;
            _checkVersion = checkVersion;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);
            var tableName = _tablePrefix + documentTable;
            var updateCmd = "update " + dialect.QuoteForTableName(tableName)
                + " set " + dialect.QuoteForColumnName("Content") + " = " + dialect.QuoteForParameter("Content") +", "
                + dialect.QuoteForColumnName("Version")  + " = " + dialect.QuoteForParameter("Version")+" where "
                + dialect.QuoteForColumnName("Id") + " = " + dialect.QuoteForParameter("Id") 
                + (_checkVersion > -1 ? " and " + dialect.QuoteForColumnName("Version") + " = " + dialect.GetSqlValue(_checkVersion) + dialect.StatementEnd : dialect.StatementEnd);

            logger.LogTrace(updateCmd);
            var parameters = dialect.GetDynamicParameters(connection, Document, tableName);
            var updatedCount = await connection.ExecuteAsync(updateCmd, parameters, transaction);

            if (_checkVersion > -1 && updatedCount != 1)
            {
                throw new ConcurrencyException();
            }
        }
    }
}
