using System;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace YesSql.Services
{
    /// <summary>
    /// This class manages a linear identifiers block allocator
    /// c.f., http://literatejava.com/hibernate/linear-block-allocator-a-superior-alternative-to-hilo/
    /// </summary>
    public class DbBlockIdGenerator : IBlockIdGenerator
    {
        private static object _synLock = new object();

        public static string TableName => "Identifiers";
        public readonly int MaxRetries = 20;

        private readonly int _range;
        private bool _initialized;
        private readonly ISqlDialect _dialect;
        private long _start;
        private int _increment;
        private long _end;
        private string _tablePrefix;

        private readonly string SelectCommand;
        private readonly string UpdateCommand;
        private readonly string InsertCommand;
        private readonly IStore _store;

        public DbBlockIdGenerator(IStore store)
        {
            _dialect = SqlDialectFactory.For(store.Configuration.ConnectionFactory.DbConnectionType);
            _range = store.Configuration.IdBlockSize;
            _tablePrefix = store.Configuration.TablePrefix;
            _store = store;

            SelectCommand = "SELECT " + _dialect.QuoteForColumnName("nextval") + " FROM " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " WHERE " + _dialect.QuoteForTableName("dimension") + " = @dimension;";
            UpdateCommand = "UPDATE " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " SET " + _dialect.QuoteForColumnName("nextval") + "=@new WHERE " + _dialect.QuoteForColumnName("nextval") + " = @previous AND " + _dialect.QuoteForColumnName("dimension") + " = @dimension;";
            InsertCommand = "INSERT INTO " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " (" + _dialect.QuoteForColumnName("dimension") + ", " + _dialect.QuoteForColumnName("nextval") + ") VALUES(@dimension, @nextval);";
        }

        public long GetNextId(string dimension)
        {
            lock (_synLock)
            {
                // Initialize the range
                if (_end == 0)
                {
                    var connection = _store.Configuration.ConnectionFactory.CreateConnection() as DbConnection;

                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }

                    try
                    {
                        EnsureInitialized(connection, dimension);
                        LeaseRange(connection, dimension);
                    }
                    finally
                    {
                        _store.Configuration.ConnectionFactory.CloseConnection(connection);
                    }
                }

                var newIncrement = Interlocked.Increment(ref _increment);
                var nextId = newIncrement + _start;

                if (nextId > _end)
                {
                    var connection = _store.Configuration.ConnectionFactory.CreateConnection() as DbConnection;

                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }

                    try
                    {
                        LeaseRange(connection, dimension);
                        nextId = GetNextId(dimension);
                    }
                    finally
                    {
                        _store.Configuration.ConnectionFactory.CloseConnection(connection);
                    }
                }

                return nextId;
            }
        }

        private void LeaseRange(DbConnection connection, string dimension)
        {
            var affectedRows = 0;
            long nextval;
            var retries = 0;

            try
            {
                do
                {
                    // Ensure we overwrite the value that has been read by this
                    // instance in case another client is trying to lease a range
                    // at the same time
                    var transaction = connection.BeginTransaction();

                    using (transaction)
                    {
                        try
                        {
                            var selectCommand = connection.CreateCommand();
                            selectCommand.CommandText = SelectCommand;

                            var selectDimension = selectCommand.CreateParameter();
                            selectDimension.Value = dimension;
                            selectDimension.ParameterName = "@dimension";
                            selectCommand.Parameters.Add(selectDimension);

                            selectCommand.Transaction = transaction;

                            nextval = Convert.ToInt64(selectCommand.ExecuteScalar());

                            var updateCommand = connection.CreateCommand();
                            updateCommand.CommandText = UpdateCommand;

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
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
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
                _store.Configuration.ConnectionFactory.CloseConnection(connection);
            }
        }

        private void EnsureInitialized(DbConnection connection, string dimension)
        {
            if (_initialized)
            {
                return;
            }

            if (_initialized)
            {
                return;
            }

            var transaction = connection.BeginTransaction();

            try
            {
                using (transaction)
                {
                    // Does the record already exist?
                    var selectCommand = transaction.Connection.CreateCommand();
                    selectCommand.CommandText = SelectCommand;

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

                    var command = transaction.Connection.CreateCommand();
                    command.CommandText = InsertCommand;

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

                    _initialized = true;
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
