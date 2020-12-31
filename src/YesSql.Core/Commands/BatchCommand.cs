using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Utils;

namespace YesSql.Commands
{
    public class BatchCommand : IIndexCommand
    {
        public static int DefaultBuilderCapacity = 10 * 1024;

        // Dedicated pool since batches should be of the same size
        private static ObjectPool<StringBuilderPool> _batchPool = StringBuilderPool.CreatePool(8, DefaultBuilderCapacity);

        public List<string> Queries { get; set; } = new List<string>();
        public DynamicParameters Parameters { get; set; } = new DynamicParameters();
        public List<Action<DbDataReader>> Actions = new List<Action<DbDataReader>>();
        public int ExecutionOrder => 0;

        public bool AddToBatch(ISqlDialect dialect, List<string> queries, DynamicParameters parameters, List<Action<DbDataReader>> actions)
        {
            if (queries == Queries)
            {
                queries.AddRange(Queries);
                foreach (var name in parameters.ParameterNames)
                {
                    Parameters.Add(name, parameters.Get<object>(name));
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

            using (var sb = _batchPool.Allocate())
            {
                foreach (var query in Queries)
                {
                    sb.Builder.AppendLine(query);
                }

                var command = sb.ToString();

                logger.LogTrace(command);

                if (command.Length > DefaultBuilderCapacity)
                {
                    logger.LogWarning("The default capacity of the BatchCommand StringBuilder {Default} might not be sufficient. It can be increased with BatchCommand.DefaultBuilderCapacity to at least {Suggested}", DefaultBuilderCapacity, command.Length);
                }

                using (var dr = await connection.ExecuteReaderAsync(command, Parameters, transaction))
                {
                    foreach (var action in Actions)
                    {
                        action(dr);
                    }
                }
            }
        }
    }
}
