using System;
using System.Threading;
using System.Threading.Tasks;

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

        private long _start;
        private int _increment;
        private long _end;
        private string _dimension;

        private string _tablePrefix;

        public LinearBlockIdGenerator(IConnectionFactory connectionFactory, int range, string dimension, string tablePrefix)
        {
            _connectionFactory = connectionFactory;
            _range = range;
            _tablePrefix = tablePrefix;
            _dimension = dimension;
        }
        
        public long GetNextId()
        {
            // Initialize the range
            if(_end == 0)
            {
                EnsureInitialized();
                LeaseRange();
            }

            var newIncrement = Interlocked.Increment(ref _increment);
            var nextId = newIncrement + _start;

            if (nextId > _end)
            {
                LeaseRange();
                return GetNextId();
            }

            return nextId;
        }

        private void LeaseRange()
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
                            selectCommand.CommandText = $"SELECT nextval FROM [{_tablePrefix}{TableName}] WHERE dimension = @dimension;";

                            var selectDimension = selectCommand.CreateParameter();
                            selectDimension.Value = _dimension;
                            selectDimension.ParameterName = "@dimension";
                            selectCommand.Parameters.Add(selectDimension);

                            selectCommand.Transaction = transaction;

                            nextval = Convert.ToInt64(selectCommand.ExecuteScalar());

                            var updateCommand = connection.CreateCommand();
                            updateCommand.CommandText = $"UPDATE [{_tablePrefix}{TableName}] SET nextval=@new WHERE nextval = @previous AND dimension = @dimension;";

                            var updateDimension = updateCommand.CreateParameter();
                            updateDimension.Value = _dimension;
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

                        if(retries++ > MaxRetries)
                        {
                            throw new Exception("Too many retries while trying to lease a range for: " + _dimension);
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
                }
            }
        }

        private void EnsureInitialized()
        {
            if(_initialized)
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
                    selectCommand.CommandText = $"SELECT nextval FROM [{_tablePrefix}{TableName}] WHERE dimension = @dimension;";

                    var selectDimension = selectCommand.CreateParameter();
                    selectDimension.Value = _dimension;
                    selectDimension.ParameterName = "@dimension";
                    selectCommand.Parameters.Add(selectDimension);

                    selectCommand.Transaction = transaction;

                    var nextVal = selectCommand.ExecuteScalar();

                    if (null != nextVal)
                    {
                        return;
                    }

                    var command = connection.CreateCommand();
                    command.CommandText = $"INSERT INTO [{_tablePrefix}{TableName}] (dimension, nextval) VALUES(@dimension, @nextval);";

                    var dimensionParameter = command.CreateParameter();
                    dimensionParameter.Value = _dimension;
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
            }
        }
    }
}
