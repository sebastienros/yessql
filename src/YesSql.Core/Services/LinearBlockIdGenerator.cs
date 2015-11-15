using System;
using System.Threading;

namespace YesSql.Core.Services
{
    /// <summary>
    /// This class manages a linear identifiers block allocator
    /// c.f., http://literatejava.com/hibernate/linear-block-allocator-a-superior-alternative-to-hilo/
    /// </summary>
    public class LinearBlockIdGenerator
    {
        public static string TableName => "YesSqlIds";

        private readonly IConnectionFactory _connectionFactory;
        private readonly int _range;

        private long _start;
        private int _increment;
        private long _end;
        private string _tablePrefix;

        public LinearBlockIdGenerator(IConnectionFactory connectionFactory, int range, string tablePrefix)
        {
            _connectionFactory = connectionFactory;
            _range = range;
            _tablePrefix = tablePrefix;
        }
        
        public long GetNextId()
        {
            // Initialize the range
            if(_end == 0)
            {
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

                    do
                    {
                        // Ensure we overwrite the value that has been read by this 
                        // instance in case another client is trying to lease a range
                        // at the same time

                        var selectCommand = connection.CreateCommand();
                        selectCommand.CommandText = $"SELECT nextval FROM [{_tablePrefix}{TableName}];";
                        nextval = Convert.ToInt64(selectCommand.ExecuteScalar());

                        using (var transaction = connection.BeginTransaction())
                        {
                            var updateCommand = connection.CreateCommand();
                            updateCommand.CommandText = $"UPDATE [{_tablePrefix}{TableName}] SET nextval=@new WHERE nextval = @previous;";

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
    }
}
