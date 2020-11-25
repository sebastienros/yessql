using Dapper;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public sealed class UpdateDocumentCommand : DocumentCommand
    {
        private readonly IStore _store;
        private readonly long _checkVersion;

        public override int ExecutionOrder { get; } = 2;

        public UpdateDocumentCommand(Document document, IStore store, long checkVersion, string collection) : base(document, collection)
        {
            _store = store;
            _checkVersion = checkVersion;
        }

        public override async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = _store.Configuration.TableNameConvention.GetDocumentTable(Collection);

            var updateCmd = "update " + dialect.QuoteForTableName(_store.Configuration.TablePrefix + documentTable)
                + " set " + dialect.QuoteForColumnName("Content") + " = @Content, " + dialect.QuoteForColumnName("Version")  + " = @Version where "
                + dialect.QuoteForColumnName("Id") + " = @Id "
                + (_checkVersion > -1 ? " and " + dialect.QuoteForColumnName("Version") + " = " + dialect.GetSqlValue(_checkVersion) + ";" : ";")
                ;

            logger.LogTrace(updateCmd);

            var updatedCount = await connection.ExecuteAsync(updateCmd, Documents, transaction);

            if (_checkVersion > -1 && updatedCount != 1)
            {
                throw new ConcurrencyException();
            }

            return;
        }
    }
}
