using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    public class BatchCommand : IIndexCommand
    {
        public static int DefaultBuilderCapacity = 10 * 1024;

        public List<string> Queries { get; set; } = new List<string>();
        public DbCommand Command { get; set; } 
        public List<Action<DbDataReader>> Actions = new List<Action<DbDataReader>>();
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

        public async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger)
        {
            if (!dialect.SupportsBatching)
            {
                throw new InvalidOperationException("Batching is not supported for this dialect");
            }

            if (Queries.Count == 0)
            {
                return;
            }

            var command = String.Concat(Queries);

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace(command);
            }

            if (command.Length > DefaultBuilderCapacity)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("The default capacity of the BatchCommand StringBuilder {Default} might not be sufficient. It can be increased with BatchCommand.DefaultBuilderCapacity to at least {Suggested}", DefaultBuilderCapacity, command.Length);
                }
            }

            Command.Transaction = transaction;
            Command.CommandText = command;

            using (var dr = await Command.ExecuteReaderAsync())
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
