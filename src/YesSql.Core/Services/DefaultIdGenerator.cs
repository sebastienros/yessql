using System;
using System.Collections.Concurrent;
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

        private static ConcurrentDictionary<string, Cursor> _cursors = new ConcurrentDictionary<string, Cursor>(StringComparer.OrdinalIgnoreCase);

        private string _tablePrefix;
        private ISqlDialect _dialect;
        private readonly string _tenant;

        public DefaultIdGenerator() : this("")
        {
        }

        public DefaultIdGenerator(string tenant)
        {
            _tenant = tenant ?? "";
        }

        public long GetNextId(string collection)
        {
            collection = collection ?? "";

            if (!_cursors.TryGetValue($"{_tenant}:{collection}", out var cursor))
            {
                throw new InvalidOperationException($"The collection '{collection}' was not initialized");
            }

            lock (_synLock)
            {
                cursor.Current += 1;
                return cursor.Current;
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

        public async Task InitializeCollectionAsync(IConfiguration configuration, string collection)
        {
            // Extract the current max value from the database
            var key = $"{_tenant}:{collection}";

            if (_cursors.ContainsKey(key))
            {
                return;
            }

            using (var connection = configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
                {
                    var tableName = String.IsNullOrEmpty(collection) ? "Document" : collection + "_" + "Document";

                    var sql = "SELECT MAX(" + _dialect.QuoteForColumnName("id") + ") FROM " + _dialect.QuoteForTableName(_tablePrefix + tableName);

                    var selectCommand = transaction.Connection.CreateCommand();
                    selectCommand.CommandText = sql;
                    selectCommand.Transaction = transaction;

                    var result = await selectCommand.ExecuteScalarAsync();

                    transaction.Commit();

                    _cursors[key] = new Cursor { Key = key, Current = result == DBNull.Value ? 0 : Convert.ToInt64(result) };
                }
            }
        }

        public static void Reset(string tenant = null, string collection = null)
        {
            tenant = tenant ?? "";
            var cursor = new Cursor { Key = $"{tenant}:{collection}", Current = 0 };
            _cursors.TryUpdate(cursor.Key, cursor, cursor);
        }

        private class Cursor
        {
            public string Key { get; set; }
            public long Current { get; set; }
        }
    }
}
