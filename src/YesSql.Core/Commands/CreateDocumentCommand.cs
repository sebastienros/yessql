using Dapper;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Collections;

namespace YesSql.Commands
{
    public sealed class CreateDocumentCommand : DocumentCommand
    {
        private readonly string _tablePrefix;

        public override int ExecutionOrder { get; } = 0;

        public CreateDocumentCommand(Document document, string tablePrefix) : base(document)
        {
            _tablePrefix = tablePrefix;
        }

        public override Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            var documentTable = CollectionHelper.Current.GetPrefixedName(Store.DocumentTable);

            var tableName = _tablePrefix + documentTable;
            var insertCmd = "insert into " + dialect.QuoteForTableName(tableName) + " (" + dialect.QuoteForColumnName("Id") + ", " + dialect.QuoteForColumnName("Type") + ", " + dialect.QuoteForColumnName("Content") + ", " + dialect.QuoteForColumnName("Version") + ") " +
                "values (" + dialect.QuoteForParameter("Id") + ", " + dialect.QuoteForParameter("Type") + ", " + dialect.QuoteForParameter("Content") + ", " + dialect.QuoteForParameter("Version") + ")" + dialect.StatementEnd;
            logger.LogTrace(insertCmd);

            var parameters = dialect.GetDynamicParameters(connection, Document, tableName);
            return connection.ExecuteScalarAsync<int>(insertCmd, parameters, transaction);
        }
    }
}
