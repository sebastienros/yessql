using System;
using System.Threading;

namespace YesSql.Services
{
    /// <summary>
    /// This class manages a linear identifiers block allocator
    /// c.f., http://literatejava.com/hibernate/linear-block-allocator-a-superior-alternative-to-hilo/
    /// </summary>
    public class LinearBlockIdGenerator
    {
        public static string TableName => "Identifiers";
        public readonly int MaxRetries = 20;

        private readonly int _range;
        private bool _initialized;
        private readonly ISqlDialect _dialect;
        private long _start;
        private int _increment;
        private long _end;

        private string _tablePrefix;

        public LinearBlockIdGenerator(IConnectionFactory connectionFactory, int range, string tablePrefix)
        {
            _dialect = SqlDialectFactory.For(connectionFactory.DbConnectionType);
            _range = range;
            _tablePrefix = tablePrefix;
        }

        public long GetNextId(ISession session, string dimension)
        {
            // Initialize the range
            if (_end == 0)
            {
                EnsureInitialized(session, dimension);
                LeaseRange(session, dimension);
            }

            var newIncrement = Interlocked.Increment(ref _increment);
            var nextId = newIncrement + _start;

            if (nextId > _end)
            {
                LeaseRange(session, dimension);
                return GetNextId(session, dimension);
            }

            return nextId;
        }

        private void LeaseRange(ISession session, string dimension)
        {
            lock (this)
            {
                var affectedRows = 0;
                long nextval;
                int retries = 0;

                do
                {
                    // Ensure we overwrite the value that has been read by this
                    // instance in case another client is trying to lease a range
                    // at the same time

                    var transaction = session.DemandAsync().GetAwaiter().GetResult();
                    
                    var selectCommand = transaction.Connection.CreateCommand();
                    selectCommand.CommandText = "SELECT " + _dialect.QuoteForColumnName("nextval") + " FROM " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " WHERE " + _dialect.QuoteForTableName("dimension") + " = @dimension;";

                    var selectDimension = selectCommand.CreateParameter();
                    selectDimension.Value = dimension;
                    selectDimension.ParameterName = "@dimension";
                    selectCommand.Parameters.Add(selectDimension);

                    selectCommand.Transaction = transaction;

                    nextval = Convert.ToInt64(selectCommand.ExecuteScalar());

                    var updateCommand = transaction.Connection.CreateCommand();
                    updateCommand.CommandText = "UPDATE " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " SET " + _dialect.QuoteForColumnName("nextval") + "=@new WHERE " + _dialect.QuoteForColumnName("nextval") + " = @previous AND " + _dialect.QuoteForColumnName("dimension") + " = @dimension;";

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

                    if (retries++ > MaxRetries)
                    {
                        throw new Exception("Too many retries while trying to lease a range for: " + dimension);
                    }

                } while (affectedRows == 0);

                _increment = -1; // Start with -1 as it will be incremented
                _start = nextval;
                _end = nextval + _range - 1;
                
            }
        }

        private void EnsureInitialized(ISession session, string dimension)
        {
            if (_initialized)
            {
                return;
            }

            var transaction = session.DemandAsync().GetAwaiter().GetResult();
            
            // Does the record already exist?
            var selectCommand = transaction.Connection.CreateCommand();
            selectCommand.CommandText = "SELECT " + _dialect.QuoteForColumnName("nextval") + " FROM " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " WHERE dimension = @dimension;";

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
            command.CommandText = "INSERT INTO " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " (" + _dialect.QuoteForColumnName("dimension") + ", " + _dialect.QuoteForColumnName("nextval") + ") VALUES(@dimension, @nextval);";

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

            _initialized = true;
        }
    }
}
