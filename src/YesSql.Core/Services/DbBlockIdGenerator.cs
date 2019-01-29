using System;
using System.Collections.Generic;
using System.Data.Common;
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
        private object _synLock = new object();

        public static string TableName => "Identifiers";
        public readonly int MaxRetries = 20;

        private ISqlDialect _dialect;
        private IStore _store;

        private int _blockSize;
        private Dictionary<string, Range> _ranges = new Dictionary<string, Range>();
        private string _tablePrefix;

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

        public async Task InitializeAsync(IStore store, ISchemaBuilder builder)
        {
            _dialect = SqlDialectFactory.For(store.Configuration.ConnectionFactory.DbConnectionType);
            _tablePrefix = store.Configuration.TablePrefix;
            _store = store;

            SelectCommand = "SELECT " + _dialect.QuoteForColumnName("nextval") + " FROM " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " WHERE " + _dialect.QuoteForTableName("dimension") + " = @dimension;";
            UpdateCommand = "UPDATE " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " SET " + _dialect.QuoteForColumnName("nextval") + "=@new WHERE " + _dialect.QuoteForColumnName("nextval") + " = @previous AND " + _dialect.QuoteForColumnName("dimension") + " = @dimension;";
            InsertCommand = "INSERT INTO " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " (" + _dialect.QuoteForColumnName("dimension") + ", " + _dialect.QuoteForColumnName("nextval") + ") VALUES(@dimension, @nextval);";

            using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                await connection.OpenAsync();

                try
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        var command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandText = $"SELECT 1 FROM {_dialect.QuoteForTableName(_tablePrefix + TableName)} ;";

                        var result = await command.ExecuteScalarAsync();

                        transaction.Commit();

                        if (result != null && Convert.ToInt64(result) == 1)
                        {
                            return;
                        }
                    }
                }
                catch
                {
                    // The table might already exist
                }
            }

            builder.CreateTable(DbBlockIdGenerator.TableName, table => table
                .Column<string>("dimension", column => column.PrimaryKey().NotNull())
                .Column<ulong>("nextval")
            )
            .AlterTable(DbBlockIdGenerator.TableName, table => table
                .CreateIndex("IX_Dimension", "dimension")
            );
        }

        public long GetNextId(string collection)
        {
            lock (_synLock)
            {
                if (!_ranges.TryGetValue(collection, out var range))
                {
                    throw new InvalidOperationException($"The collection '{collection}' was not initialized");
                }

                range.Cursor += 1;
                var nextId = range.Cursor + range.Start;

                if (nextId > range.End)
                {
                    LeaseRange(range);
                    nextId = GetNextId(collection);
                }

                return nextId;
            }
        }

        private void LeaseRange(Range range)
        {
            var affectedRows = 0;
            long nextval = 0;
            var retries = 0;

            using (var connection = _store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                do
                {
                    // Ensure we overwrite the value that has been read by this
                    // instance in case another client is trying to lease a range
                    // at the same time
                    using (var transaction = connection.BeginTransaction())
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

                            nextval = Convert.ToInt64(selectCommand.ExecuteScalar());

                            var updateCommand = connection.CreateCommand();
                            updateCommand.CommandText = UpdateCommand;

                            var updateDimension = updateCommand.CreateParameter();
                            updateDimension.Value = range.Collection;
                            updateDimension.ParameterName = "@dimension";
                            updateCommand.Parameters.Add(updateDimension);

                            var newValue = updateCommand.CreateParameter();
                            newValue.Value = nextval + _blockSize;
                            newValue.ParameterName = "@new";
                            updateCommand.Parameters.Add(newValue);

                            var previousValue = updateCommand.CreateParameter();
                            previousValue.Value = nextval;
                            previousValue.ParameterName = "@previous";
                            updateCommand.Parameters.Add(previousValue);

                            updateCommand.Transaction = transaction;

                            affectedRows = updateCommand.ExecuteNonQuery();

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                        }
                    }

                    if (retries++ > MaxRetries)
                    {
                        throw new Exception("Too many retries while trying to lease a range for: " + range.Collection);
                    }

                } while (affectedRows == 0);

                range.SetBlock(nextval, _blockSize);
            }
        }

        public async Task InitializeCollectionAsync(DbTransaction transaction, string collection, ISchemaBuilder builder)
        {
            if (_ranges.ContainsKey(collection))
            {
                return;
            }

            // Does the record already exist?
            var selectCommand = transaction.Connection.CreateCommand();
            selectCommand.CommandText = SelectCommand;

            var selectDimension = selectCommand.CreateParameter();
            selectDimension.Value = collection;
            selectDimension.ParameterName = "@dimension";
            selectCommand.Parameters.Add(selectDimension);

            selectCommand.Transaction = transaction;

            var nextVal = await selectCommand.ExecuteScalarAsync();

            if (null != nextVal)
            {
                _ranges[collection] = new Range(collection).SetBlock(Convert.ToInt64(nextVal), _blockSize);

                return;
            }

            var command = transaction.Connection.CreateCommand();
            command.CommandText = InsertCommand;
            command.Transaction = transaction;

            var dimensionParameter = command.CreateParameter();
            dimensionParameter.Value = collection;
            dimensionParameter.ParameterName = "@dimension";
            command.Parameters.Add(dimensionParameter);

            var nextValParameter = command.CreateParameter();
            nextValParameter.Value = _blockSize + 1;
            nextValParameter.ParameterName = "@nextval";
            command.Parameters.Add(nextValParameter);

            await command.ExecuteNonQueryAsync();

            _ranges[collection] = new Range(collection).SetBlock(1, _blockSize);
        }

        private class Range
        {
            public Range(string collection)
            {
                Collection = collection;
            }

            public Range SetBlock(long start, int blockSize)
            {
                Start = start;
                End = Start + blockSize - 1;
                Cursor = -1;

                return this;
            }

            public string Collection;
            public long Cursor;
            public long Start;
            public long End;
        }
    }
}
