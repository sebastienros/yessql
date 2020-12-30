using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Utils;

namespace YesSql.Commands
{
    public class BachCommand : IIndexCommand
    {
        public static int DefaultBuilderCapacity = 10 * 1024;

        // Dedicate pool since batches should be of the same size
        private static ObjectPool<StringBuilderPool> _batchPool = StringBuilderPool.CreatePool(8, DefaultBuilderCapacity);

        public List<string> Queries { get; set; } = new List<string>();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public int ExecutionOrder => 0;

        public bool AddToBatch(ISqlDialect dialect, List<string> queries, Dictionary<string, object> parameters)
        {
            if (queries == Queries)
            {
                queries.AddRange(Queries);
                foreach (var entry in parameters)
                {
                    Parameters[entry.Key] = entry.Value;
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
                    logger.LogWarning("The default capacity of the BatchCommand StringBuilder {Default} might not be sufficient. It can be increased with BachCommand.DefaultBuilderCapacity to at least {Suggested}", DefaultBuilderCapacity, command.Length);
                }

                var count = await connection.ExecuteAsync(command, Parameters, transaction);
            }
        }
    }
}
