using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using YesSql.Sql;

namespace YesSql.Services
{
    /// <summary>
    /// Generates unique identifiers.
    /// </summary>
    public class DefaultIdGenerator : IIdGenerator
    {
        private object _synLock = new object();

        private Dictionary<string, long> _seeds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        private string _tablePrefix;
        private ISqlDialect _dialect;

        public long GetNextId(IDbTransaction transaction, string collection)
        {
            lock (_synLock)
            {
                collection = collection ?? "";

                if (!_seeds.TryGetValue(collection, out var seed))
                {
                    var tableName = String.IsNullOrEmpty(collection) ? "Document" : collection + "_" + "Document";

                    var sql = "SELECT MAX(" + _dialect.QuoteForColumnName("id") + ") FROM " + _dialect.QuoteForTableName(_tablePrefix + tableName);

                    var selectCommand = transaction.Connection.CreateCommand();
                    selectCommand.CommandText = sql;
                    selectCommand.Transaction = transaction;

                    var result = selectCommand.ExecuteScalar();

                    seed = result == DBNull.Value ? 0 : Convert.ToInt64(result);
                }

                return _seeds[collection] = seed + 1;
            }
        }

        public Task InitializeAsync(IStore store, ISchemaBuilder builder)
        {
            _dialect = SqlDialectFactory.For(store.Configuration.ConnectionFactory.DbConnectionType);
            _tablePrefix = store.Configuration.TablePrefix;

#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
