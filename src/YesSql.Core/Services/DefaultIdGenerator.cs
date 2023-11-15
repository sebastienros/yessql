using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YesSql.Services
{
    /// <summary>
    /// Generates unique identifiers.
    /// </summary>
    public class DefaultIdGenerator : IIdGenerator
    {
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

        private readonly Dictionary<string, long> _seeds = new(StringComparer.OrdinalIgnoreCase);

        private ISqlDialect _dialect;

        public long GetNextId(string collection)
            => GetNextIdAsync(collection).ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task<long> GetNextIdAsync(string collection)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                collection ??= string.Empty;

                if (!_seeds.TryGetValue(collection, out var seed))
                {
                    throw new InvalidOperationException($"The collection '{collection}' was not initialized");
                }

                return _seeds[collection] = seed + 1;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public Task InitializeAsync(IStore store)
        {
            _dialect = store.Configuration.SqlDialect;
            return Task.CompletedTask;
        }

        public async Task InitializeCollectionAsync(IConfiguration configuration, string collection)
        {
            // Extract the current max value from the database

            await using var connection = configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync(configuration.IsolationLevel);
            var tableName = configuration.TableNameConvention.GetDocumentTable(collection);

            var sql = "SELECT MAX(" + _dialect.QuoteForColumnName("Id") + ") FROM " + _dialect.QuoteForTableName(configuration.TablePrefix + tableName, configuration.Schema);

            var selectCommand = transaction.Connection.CreateCommand();
            selectCommand.CommandText = sql;
            selectCommand.Transaction = transaction;

            if (configuration.Logger.IsEnabled(LogLevel.Trace))
            {
                configuration.Logger.LogTrace(sql);
            }
            var result = await selectCommand.ExecuteScalarAsync();

            await transaction.CommitAsync();

            _seeds[collection] = result == DBNull.Value ? 0 : Convert.ToInt64(result);
        }
    }
}
