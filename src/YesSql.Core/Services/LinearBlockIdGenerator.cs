using System;
using System.Data.Common;
using System.Threading;
using YesSql.Core.Sql;

namespace YesSql.Core.Services
{
    /// <summary>
    /// This class manages a linear identifiers block allocator
    /// c.f., http://literatejava.com/hibernate/linear-block-allocator-a-superior-alternative-to-hilo/
    /// </summary>
    public class LinearBlockIdGenerator
    {
        public static string TableName => "Identifiers";
        public readonly int MaxRetries = 20;

        private readonly IConnectionFactory _connectionFactory;
        private readonly int _range;
        private bool _initialized;
        private readonly ISqlDialect _dialect;
        private long _start;
        private int _increment;
        private long _end;
        private char _openQuoteDialect;
        private char _closeQuoteDialect;

        private string _tablePrefix;

        public LinearBlockIdGenerator(IConnectionFactory connectionFactory, int range, string tablePrefix)
        {
            _connectionFactory = connectionFactory;
            _dialect = SqlDialectFactory.For(connectionFactory.CreateConnection());
            _range = range;
            _tablePrefix = tablePrefix;
            _openQuoteDialect = _dialect.OpenQuote;
            _closeQuoteDialect = _dialect.CloseQuote;
        }

        public long GetNextId(string dimension)
        {
            // Initialize the range
            if (_end == 0)
            {
                EnsureInitialized(dimension);
                LeaseRange(dimension);
            }

            var newIncrement = Interlocked.Increment(ref _increment);
            var nextId = newIncrement + _start;

            if (nextId > _end)
            {
                LeaseRange(dimension);
                return GetNextId(dimension);
            }

            return nextId;
        }

        private void LeaseRange(string dimension)
        {
            lock (this)
            {
                var connection = _connectionFactory.CreateConnection();
                connection.Open();
                try
                {
                    var affectedRows = 0;
                    long nextval;
                    int retries = 0;

                    do
                    {
                        // Ensure we overwrite the value that has been read by this
                        // instance in case another client is trying to lease a range
                        // at the same time

                        using (var transaction = connection.BeginTransaction())
                        {
                            var selectCommand = connection.CreateCommand();
                            selectCommand.CommandText = $"SELECT nextval FROM {_openQuoteDialect}{_tablePrefix}{TableName}{_closeQuoteDialect} WHERE dimension = @dimension;";

                            var selectDimension = selectCommand.CreateParameter();
                            selectDimension.Value = dimension;
                            selectDimension.ParameterName = "@dimension";
                            selectCommand.Parameters.Add(selectDimension);

                            selectCommand.Transaction = transaction;

                            nextval = Convert.ToInt64(selectCommand.ExecuteScalar());

                            var updateCommand = connection.CreateCommand();
                            updateCommand.CommandText = $"UPDATE {_openQuoteDialect}{_tablePrefix}{TableName}{_closeQuoteDialect} SET nextval=@new WHERE nextval = @previous AND dimension = @dimension;";

                            var updateDimension = updateCommand.CreateParameter();
                            updateDimension.Value = dimension;
                            updateDimension.ParameterName = "@dimension";
                            updateCommand.Parameters.Add(updateDimension);

                            var newValue = updateCommand.CreateParameter();
                            newValue.Value = nextval + _range;
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

                        if (retries++ > MaxRetries)
                        {
                            throw new Exception("Too many retries while trying to lease a range for: " + dimension);
                        }

                    } while (affectedRows == 0);

                    _increment = -1; // Start with -1 as it will be incremented
                    _start = nextval;
                    _end = nextval + _range - 1;

                }
                finally
                {
                    if (_connectionFactory.Disposable)
                    {
                        connection.Dispose();
                    }
                    else
                    {
                        connection.Close();
                    }
                }
            }
        }

        private void EnsureInitialized(string dimension)
        {
            if (_initialized)
            {
                return;
            }

            var connection = _connectionFactory.CreateConnection();
            connection.Open();
            try
            {
                using (var transaction = connection.BeginTransaction())
                {
                    // Does the record already exist?
                    var selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = $"SELECT nextval FROM {_openQuoteDialect}{_tablePrefix}{TableName}{_closeQuoteDialect} WHERE dimension = @dimension;";

                    var selectDimension = selectCommand.CreateParameter();
                    selectDimension.Value = dimension;
                    selectDimension.ParameterName = "@dimension";
                    selectCommand.Parameters.Add(selectDimension);

                    selectCommand.Transaction = transaction;

                    var nextVal = selectCommand.ExecuteScalar();

                    if (null != nextVal)
                    {
                        return;
                    }

                    var command = connection.CreateCommand();
                    command.CommandText = $"INSERT INTO {_openQuoteDialect}{_tablePrefix}{TableName}{_closeQuoteDialect} (dimension, nextval) VALUES(@dimension, @nextval);";

                    var dimensionParameter = command.CreateParameter();
                    dimensionParameter.Value = dimension;
                    dimensionParameter.ParameterName = "@dimension";
                    command.Parameters.Add(dimensionParameter);

                    var nextValParameter = command.CreateParameter();
                    nextValParameter.Value = 1;
                    nextValParameter.ParameterName = "@nextval";
                    command.Parameters.Add(nextValParameter);

                    command.Transaction = transaction;

                    command.ExecuteNonQuery();
                    transaction.Commit();
                }

                _initialized = true;
            }
            finally
            {
                if (_connectionFactory.Disposable)
                {
                    connection.Dispose();
                }
                else
                {
                    connection.Close();
                }
            }
        }
    }
}
