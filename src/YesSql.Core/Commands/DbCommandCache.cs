using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    /// <summary>
    /// Holds the database commands executed individually while a session is flushed, so that a statement executed
    /// for many documents or index entries is created and prepared by the driver only once, then executed again
    /// with new parameter values.
    /// </summary>
    /// <remarks>
    /// Commands are keyed by their SQL text. A command using a given SQL text must always define the same parameters.
    /// Some drivers (e.g. Microsoft.Data.Sqlite) parse and prepare the statements of a command each time a new
    /// command is executed, which is significantly more expensive than executing a prepared statement again.
    /// </remarks>
    internal sealed class DbCommandCache : IDisposable
    {
        private readonly DbConnection _connection;
        private readonly DbTransaction _transaction;
        private readonly Dictionary<string, DbCommand> _commands = new(StringComparer.Ordinal);

        public DbCommandCache(DbConnection connection, DbTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        public DbConnection Connection => _connection;

        /// <summary>
        /// Returns the command for a SQL text, creating it if necessary.
        /// </summary>
        /// <param name="sql">The SQL text of the command.</param>
        /// <param name="created"><c>true</c> if the command was created, in which case its parameters need to be added.</param>
        public DbCommand GetCommand(string sql, out bool created)
        {
            if (_commands.TryGetValue(sql, out var command))
            {
                created = false;
                return command;
            }

            command = _connection.CreateCommand();
            command.Transaction = _transaction;
            command.CommandText = sql;

            _commands.Add(sql, command);
            created = true;

            return command;
        }

        public void Dispose()
        {
            foreach (var command in _commands.Values)
            {
                command.Dispose();
            }

            _commands.Clear();
        }

        /// <summary>
        /// Adds a parameter without a value to a command.
        /// </summary>
        public static DbParameter AddParameter(DbCommand command, string name, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = dbType;
            command.Parameters.Add(parameter);

            return parameter;
        }

        /// <summary>
        /// Sets the value of a string parameter the same way Dapper does, so that the parameters of a reused command
        /// get the same size as the ones Dapper would create (which matters for the query plans of some databases).
        /// </summary>
        public static void SetStringValue(DbParameter parameter, string value)
        {
            parameter.Size = value is null || value.Length > DefaultStringLength ? -1 : DefaultStringLength;
            parameter.Value = (object)value ?? DBNull.Value;
        }

        // Dapper's default size for string parameters (DbString.DefaultLength).
        private const int DefaultStringLength = 4000;
    }

    /// <summary>
    /// A command that can be executed with a command from a <see cref="DbCommandCache"/>, instead of creating a new one.
    /// </summary>
    internal interface ICachedCommand
    {
        Task ExecuteAsync(DbCommandCache commandCache, DbTransaction transaction, ISqlDialect dialect, ILogger logger, CancellationToken cancellationToken);
    }
}
