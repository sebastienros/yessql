using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public class BatchCommand : IIndexCommand
    {
        [Obsolete("The field is not used anymore since the current batching implementation doesn't need it.")]
        public static readonly int DefaultBuilderCapacity = 10 * 1024;

        public List<string> Queries { get; set; } = new List<string>();
        public DbCommand Command { get; set; }
        public List<Action<DbDataReader>> Actions = new();
        public int ExecutionOrder => 0;

        public BatchCommand(DbCommand command)
        {
            Command = command;
        }

        public bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand batchCommand, List<Action<DbDataReader>> actions, int index)
        {
            if (queries == Queries)
            {
                queries.AddRange(Queries);
                foreach (var parameter in Command.Parameters)
                {
                    batchCommand.Parameters.Add(parameter);
                }
            }

            return true;
        }

        public async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger, CancellationToken cancellationToken)
        {
            if (!dialect.SupportsBatching)
            {
                throw new InvalidOperationException("Batching is not supported for this dialect");
            }

            if (Queries.Count == 0)
            {
                return;
            }

            var command = string.Concat(Queries);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(command);
            }

            Command.Transaction = transaction;
            Command.CommandText = command;

            // This should propigate the cancellation token to any of the actions.
            await using (var dr = await Command.ExecuteReaderAsync(cancellationToken))
            {
                foreach (var action in Actions)
                {
                    action(dr);
                }
            }
        }
    }

    public static class CommandExtensions
    {
        public static DbCommand AddParameter(this DbCommand command, string name, object value, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            parameter.DbType = dbType;

            command.Parameters.Add(parameter);
            return command;
        }

        public static DbCommand AddParameter(this DbCommand command, string name, int value)
        {
            return AddParameter(command, name, value, DbType.Int32);
        }

        public static DbCommand AddParameter(this DbCommand command, string name, long value)
        {
            return AddParameter(command, name, value, DbType.Int64);
        }

        public static DbCommand AddParameter(this DbCommand command, string name, string value)
        {
            return AddParameter(command, name, value, DbType.String);
        }
    }
}
