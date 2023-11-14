using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YesSql.Sql;

namespace YesSql.Services
{
    /// <summary>
    /// This class manages a linear identifiers block allocator
    /// c.f., http://literatejava.com/hibernate/linear-block-allocator-a-superior-alternative-to-hilo/
    /// </summary>
    public class DbBlockIdGenerator : IIdGenerator
    {
        internal long _initialValue = 1;
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

        public static string TableName => "Identifiers";
        public readonly int MaxRetries = 20;

        private ISqlDialect _dialect;
        private IStore _store;

        private readonly int _blockSize;
        private readonly Dictionary<string, Range> _ranges = new();
        private string _tablePrefix;
        private string _schema;

        private string SelectCommand;
        private string UpdateCommand;
        private string InsertCommand;

        public DbBlockIdGenerator() : this(20)
        {
        }

        public DbBlockIdGenerator(int blockSize)
        {
            _blockSize = blockSize;
        }

        public async Task InitializeAsync(IStore store)
        {
            _store = store;
            _dialect = store.Configuration.SqlDialect;
            _tablePrefix = store.Configuration.TablePrefix;
            _schema = store.Configuration.Schema;

            SelectCommand = "SELECT " + _dialect.QuoteForColumnName("nextval") + " FROM " + _dialect.QuoteForTableName(_tablePrefix + TableName, _schema) + " WHERE " + _dialect.QuoteForColumnName("dimension") + " = @dimension;";
            UpdateCommand = "UPDATE " + _dialect.QuoteForTableName(_tablePrefix + TableName, _schema) + " SET " + _dialect.QuoteForColumnName("nextval") + "=@new WHERE " + _dialect.QuoteForColumnName("nextval") + " = @previous AND " + _dialect.QuoteForColumnName("dimension") + " = @dimension;";
            InsertCommand = "INSERT INTO " + _dialect.QuoteForTableName(_tablePrefix + TableName, _schema) + " (" + _dialect.QuoteForColumnName("dimension") + ", " + _dialect.QuoteForColumnName("nextval") + ") VALUES(@dimension, @nextval);";

            await using var connection = store.Configuration.ConnectionFactory.CreateConnection();

            await connection.OpenAsync();
            var transaction = await connection.BeginTransactionAsync(store.Configuration.IsolationLevel);
            try
            {
                var localBuilder = new SchemaBuilder(store.Configuration, transaction, true);

                await localBuilder.CreateTableAsync(TableName, table => table
                    .Column<string>("dimension", column => column.PrimaryKey().NotNull())
                    .Column<long>("nextval")
                    );

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }

        public long GetNextId(string collection)
            => GetNextIdAsync(collection).ConfigureAwait(false).GetAwaiter().GetResult();

        public async Task<long> GetNextIdAsync(string collection)
        {
            collection ??= string.Empty;

            await _semaphoreSlim.WaitAsync();

            try
            {
                if (!_ranges.TryGetValue(collection, out var range))
                {
                    throw new InvalidOperationException($"The collection '{collection}' was not initialized");
                }

                var nextId = range.Next();

                if (nextId > range.End)
                {
                    await LeaseRangeAsync(range);
                    nextId = range.Next();
                }

                return nextId;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task LeaseRangeAsync(Range range)
        {
            var affectedRows = 0;
            long nextValue = 0;
            var retries = 0;

            await using var connection = _store.Configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            do
            {
                // Ensure we overwrite the value that has been read by this
                // instance in case another client is trying to lease a range
                // at the same time
                await using (var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        var selectCommand = connection.CreateCommand();
                        selectCommand.CommandText = SelectCommand;

                        var selectDimension = selectCommand.CreateParameter();
                        selectDimension.Value = range.Collection;
                        selectDimension.ParameterName = "@dimension";
                        selectCommand.Parameters.Add(selectDimension);

                        selectCommand.Transaction = transaction;

                        if (_store.Configuration.Logger.IsEnabled(LogLevel.Trace))
                        {
                            _store.Configuration.Logger.LogTrace(SelectCommand);
                        }

                        nextValue = Convert.ToInt64(await selectCommand.ExecuteScalarAsync());

                        var updateCommand = connection.CreateCommand();
                        updateCommand.CommandText = UpdateCommand;

                        var updateDimension = updateCommand.CreateParameter();
                        updateDimension.Value = range.Collection;
                        updateDimension.ParameterName = "@dimension";
                        updateCommand.Parameters.Add(updateDimension);

                        var newValue = updateCommand.CreateParameter();
                        newValue.Value = nextValue + _blockSize;
                        newValue.ParameterName = "@new";
                        updateCommand.Parameters.Add(newValue);

                        var previousValue = updateCommand.CreateParameter();
                        previousValue.Value = nextValue;
                        previousValue.ParameterName = "@previous";
                        updateCommand.Parameters.Add(previousValue);

                        updateCommand.Transaction = transaction;

                        if (_store.Configuration.Logger.IsEnabled(LogLevel.Trace))
                        {
                            _store.Configuration.Logger.LogTrace(UpdateCommand);
                        }
                        affectedRows = await updateCommand.ExecuteNonQueryAsync();

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        affectedRows = 0;
                        await transaction.RollbackAsync();
                    }
                }

                if (retries++ > MaxRetries)
                {
                    throw new Exception("Too many retries while trying to lease a range for: " + range.Collection);
                }

            } while (affectedRows == 0);

            range.SetBlock(nextValue, _blockSize);
        }

        public async Task InitializeCollectionAsync(IConfiguration configuration, string collection)
        {
            if (_ranges.ContainsKey(collection))
            {
                return;
            }

            object nextval;

            await using var connection = configuration.ConnectionFactory.CreateConnection();
            await connection.OpenAsync();

            await using (var transaction = await connection.BeginTransactionAsync(configuration.IsolationLevel))
            {
                // Does the record already exist?
                var selectCommand = transaction.Connection.CreateCommand();
                selectCommand.CommandText = SelectCommand;

                var selectDimension = selectCommand.CreateParameter();
                selectDimension.Value = collection;
                selectDimension.ParameterName = "@dimension";
                selectCommand.Parameters.Add(selectDimension);

                selectCommand.Transaction = transaction;

                if (_store.Configuration.Logger.IsEnabled(LogLevel.Trace))
                {
                    _store.Configuration.Logger.LogTrace(SelectCommand);
                }

                nextval = await selectCommand.ExecuteScalarAsync();

                await transaction.CommitAsync();
            }

            if (nextval == null)
            {
                // Try to create a new record. If it fails, retry reading the record.
                try
                {
                    await using var transaction = await connection.BeginTransactionAsync(configuration.IsolationLevel);
                    // To prevent concurrency issues when creating this record (it must be unique)
                    // we generate a random collection name, then update it safely

                    var command = transaction.Connection.CreateCommand();
                    command.CommandText = InsertCommand;
                    command.Transaction = transaction;

                    var dimensionParameter = command.CreateParameter();
                    dimensionParameter.Value = collection;
                    dimensionParameter.ParameterName = "@dimension";
                    command.Parameters.Add(dimensionParameter);

                    var nextValParameter = command.CreateParameter();
                    nextValParameter.Value = _initialValue;
                    nextValParameter.ParameterName = "@nextval";
                    command.Parameters.Add(nextValParameter);

                    if (_store.Configuration.Logger.IsEnabled(LogLevel.Trace))
                    {
                        _store.Configuration.Logger.LogTrace(InsertCommand);
                    }

                    await command.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await InitializeCollectionAsync(configuration, collection);
                }
            }

            _ranges[collection] = new Range(collection);
        }

        private class Range
        {
            public Range(string collection)
            {
                Collection = collection;
                Cursor = 1;
            }

            public Range SetBlock(long start, int blockSize)
            {
                Start = start;
                End = Start + blockSize - 1;
                Cursor = 0;

                return this;
            }

            public long Next()
            {
                return Start + Cursor++;
            }

            public string Collection;
            public long Cursor;
            public long Start;
            public long End;
        }
    }
}
