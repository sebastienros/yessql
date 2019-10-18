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

            var updateCmd = "update " + dialect.QuoteForTableName(_tablePrefix + documentTable)
                + " set " + dialect.QuoteForColumnName("Content") + " = @Content, " + dialect.QuoteForColumnName("Version")  + " = @Version where "
                + dialect.QuoteForColumnName("Id") + " = @Id "
                + (_checkVersion > -1 ? " and " + dialect.QuoteForColumnName("Version") + " = " + dialect.GetSqlValue(_checkVersion) + ";" : ";")
                ;

            logger.LogTrace(updateCmd);

            var updatedCount = await connection.ExecuteAsync(updateCmd, Document, transaction);

            if (_checkVersion > -1 && updatedCount != 1)
            {
                throw new ConcurrencyException();
            }

            return;
        }
    }
}
