using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace YesSql.Commands
{
    /// <summary>
    /// A command recording its database operations in the statistics of the session executing it.
    /// </summary>
    internal interface IInstrumentedCommand
    {
        YesSqlStatistics Statistics { set; }
    }

    public interface IIndexCommand
    {
        Task ExecuteAsync(DbConnection connection, DbTransaction transaction, ISqlDialect dialect, ILogger logger, CancellationToken cancellationToken = default);
        bool AddToBatch(ISqlDialect dialect, List<string> queries, DbCommand batchCommand, List<Action<DbDataReader>> actions, int index);
        int ExecutionOrder { get; }
    }
}