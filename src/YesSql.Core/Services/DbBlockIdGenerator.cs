using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
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
        private HashSet<string> _initializedCollections = new HashSet<string>();
        private long _start;
        private int _increment;
        private long _end;
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

        public Task InitializeAsync(IStore store, ISchemaBuilder builder)
        {
            _dialect = SqlDialectFactory.For(store.Configuration.ConnectionFactory.DbConnectionType);
            _tablePrefix = store.Configuration.TablePrefix;
            _store = store;

            SelectCommand = "SELECT " + _dialect.QuoteForColumnName("nextval") + " FROM " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " WHERE " + _dialect.QuoteForTableName("dimension") + " = @dimension;";
            UpdateCommand = "UPDATE " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " SET " + _dialect.QuoteForColumnName("nextval") + "=@new WHERE " + _dialect.QuoteForColumnName("nextval") + " = @previous AND " + _dialect.QuoteForColumnName("dimension") + " = @dimension;";
            InsertCommand = "INSERT INTO " + _dialect.QuoteForTableName(_tablePrefix + TableName) + " (" + _dialect.QuoteForColumnName("dimension") + ", " + _dialect.QuoteForColumnName("nextval") + ") VALUES(@dimension, @nextval);";

            builder.CreateTable(DbBlockIdGenerator.TableName, table => table
                .Column<string>("dimension", column => column.PrimaryKey().NotNull())
                .Column<ulong>("nextval")
            )
            .AlterTable(DbBlockIdGenerator.TableName, table => table
                .CreateIndex("IX_Dimension", "dimension")
            );

#if NET451
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        public long GetNextId(IDbTransaction transaction, string dimension)
        {
            // Initialize the range
            if (_end == 0)
            {
                EnsureInitialized(dimension);
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
                    nextId = GetNextId(transaction, dimension);
                }
                finally
                {
                    _store.Configuration.ConnectionFactory.CloseConnection(connection);
                }
            }

            return nextId;
        }

        private void LeaseRange(DbConnection connection, string dimension)
        {
            var affectedRows = 0;
            long nextval;
            var retries = 0;

            lock (_synLock)
            {
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
                    _end = nextval + _blockSize - 1;
                }
                finally
                {
                    _store.Configuration.ConnectionFactory.CloseConnection(connection);
                }
            }
        }

        private void EnsureInitialized(string dimension)
        {
            if (_initializedCollections.Contains(dimension))
            {
                return;
            }

            lock (_synLock)
            {
                // Create a specific connection
                var connection = _store.Configuration.ConnectionFactory.CreateConnection() as DbConnection;

                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

                var transaction = connection.BeginTransaction();
                try
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

                    // Copy the current collection to preserve thread-safety
                    var newList = new HashSet<string>(_initializedCollections);
                    _initializedCollections = newList;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    _store.Configuration.ConnectionFactory.CloseConnection(connection);
                }
            }
        }
    }
}
