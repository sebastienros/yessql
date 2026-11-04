using System;
using System.Threading;

namespace YesSql
{
    /// <summary>
    /// The totals of the database work of a session or of a <see cref="YesSqlTelemetryScope"/>.
    /// </summary>
    /// <remarks>
    /// A high number of <see cref="Commands"/> for the same unit of work reveals an N+1 query pattern.
    /// A query served by query gating runs once, and counts for the session that ran it.
    /// The id generators use their own connections, so their commands count for a scope but not for a session.
    /// </remarks>
    public sealed class YesSqlStatistics
    {
        internal enum Counter
        {
            Commands,
            CommandDurationTicks,
            Flushes,
            DocumentsRead,
            DocumentsInserted,
            DocumentsUpdated,
            DocumentsDeleted,
            Count,
        }

        private readonly long[] _counters = new long[(int)Counter.Count];

        /// <summary>
        /// Gets the number of database commands that were executed, without the commits and rollbacks. A batch counts as one.
        /// </summary>
        public long Commands => Get(Counter.Commands);

        /// <summary>
        /// Gets the time spent executing the <see cref="Commands"/>.
        /// </summary>
        /// <remarks>
        /// Timing a command is only done while something asks for it, so this value is always available from a
        /// <see cref="YesSqlTelemetryScope"/>, and from a session only while a scope is active or
        /// <see cref="YesSqlTelemetry.Name"/> has a listener.
        /// </remarks>
        public TimeSpan CommandDuration => TimeSpan.FromTicks(Get(Counter.CommandDurationTicks));

        /// <summary>
        /// Gets the number of flushes that had work to look at, whether explicit or triggered by a query.
        /// </summary>
        public long Flushes => Get(Counter.Flushes);

        /// <summary>
        /// Gets the number of documents returned by the queries, including the ones already tracked by a session.
        /// </summary>
        public long DocumentsRead => Get(Counter.DocumentsRead);

        /// <summary>
        /// Gets the number of documents inserted by the flushes.
        /// </summary>
        public long DocumentsInserted => Get(Counter.DocumentsInserted);

        /// <summary>
        /// Gets the number of documents updated by the flushes.
        /// </summary>
        public long DocumentsUpdated => Get(Counter.DocumentsUpdated);

        /// <summary>
        /// Gets the number of documents deleted by the flushes.
        /// </summary>
        public long DocumentsDeleted => Get(Counter.DocumentsDeleted);

        /// <summary>
        /// Adds a value to a counter of some statistics, if any, and of the active scopes.
        /// </summary>
        internal static void Add(YesSqlStatistics statistics, Counter counter, long value = 1)
        {
            statistics?.Increment(counter, value);

            for (var scope = YesSqlTelemetryScope.Current; scope != null; scope = scope.Parent)
            {
                scope.Statistics.Increment(counter, value);
            }
        }

        internal void Increment(Counter counter, long value = 1)
            => Interlocked.Add(ref _counters[(int)counter], value);

        private long Get(Counter counter)
            => Interlocked.Read(ref _counters[(int)counter]);
    }
}
