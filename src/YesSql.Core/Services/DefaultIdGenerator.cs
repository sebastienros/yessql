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
            => GetNextIdAsync(collection).GetAwaiter().GetResult();

        public async Task<long> GetNextIdAsync(string collection, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
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

        public Task InitializeAsync(IStore store, CancellationToken cancellationToken = default)
        {
            _dialect = store.Configuration.SqlDialect;
            return Task.CompletedTask;
        }

        public Task InitializeAsync(IStore store)
            => InitializeAsync(store, CancellationToken.None);

        public async Task InitializeCollectionAsync(IConfiguration configuration, string collection, CancellationToken cancellationToken = default)
        {
            // Extract the current max value from the database

            await using var connection = configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(configuration.IsolationLevel, cancellationToken);
            var tableName = configuration.TableNameConvention.GetDocumentTable(collection);

            var sql = "SELECT MAX(" + _dialect.QuoteForColumnName("Id") + ") FROM " + _dialect.QuoteForTableName(configuration.TablePrefix + tableName, configuration.Schema);

            var selectCommand = transaction.Connection.CreateCommand();
            selectCommand.CommandText = sql;
            selectCommand.Transaction = transaction;

            if (configuration.Logger.IsEnabled(LogLevel.Trace))
            {
                configuration.Logger.LogTrace(sql);
            }
            var result = await selectCommand.ExecuteScalarAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _seeds[collection] = result == DBNull.Value ? 0 : Convert.ToInt64(result);
        }

        public Task InitializeCollectionAsync(IConfiguration configuration, string collection)
            => InitializeCollectionAsync(configuration, collection, CancellationToken.None);

        public Task<long> GetNextIdAsync(string collection)
            => GetNextIdAsync(collection, CancellationToken.None);
    }
}
