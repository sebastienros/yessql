using Dapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Counter = YesSql.YesSqlStatistics.Counter;

namespace YesSql.Diagnostics
{
    /// <summary>
    /// Records the spans, measurements and statistics of the work of YesSql.
    /// </summary>
    /// <remarks>
    /// Database operations are instrumented where YesSql runs them rather than by wrapping the provider's connection, so
    /// that the provider's own connection, command and transaction objects keep being the ones in use.
    /// </remarks>
    internal static class YesSqlInstrumentation
    {
        private const string DbSystemName = "db.system.name";
        private const string DbNamespace = "db.namespace";
        private const string DbOperationName = "db.operation.name";
        private const string DbCollectionName = "db.collection.name";
        private const string DbQueryText = "db.query.text";
        private const string DbOperationBatchSize = "db.operation.batch.size";
        private const string ErrorType = "error.type";
        private const string DocumentOperation = "yessql.document.operation";
        private const string FlushSpanName = "yessql.session.flush";

        private static readonly Histogram<double> _operationDuration = CreateDurationHistogram(
            "db.client.operation.duration",
            "Duration of database client operations.");

        private static readonly Histogram<double> _flushDuration = CreateDurationHistogram(
            "yessql.session.flush.duration",
            "Duration of the flushes of a session.");

        private static readonly Histogram<long> _sessionCommands = YesSqlTelemetry.Meter.CreateHistogram<long>(
            "yessql.session.commands",
            "{command}",
            "Number of database commands executed by a session.");

        private static readonly Histogram<long> _sessionDocuments = YesSqlTelemetry.Meter.CreateHistogram<long>(
            "yessql.session.documents",
            "{document}",
            "Number of documents read or written by a session.");

        private static readonly YesSqlTelemetryOptions _defaultOptions = new();

        // A source with the same name as the one of a driver: it never emits, but it has listeners when the driver's has.
        private static readonly ConcurrentDictionary<string, ActivitySource> _driverSources = new();

        private static Histogram<double> CreateDurationHistogram(string name, string description)
        {
#if NET9_0_OR_GREATER
            // The boundaries recommended by the OpenTelemetry semantic conventions for 'db.client.operation.duration'.
            return YesSqlTelemetry.Meter.CreateHistogram(
                name,
                "s",
                description,
                tags: null,
                advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1, 5, 10] });
#else
            return YesSqlTelemetry.Meter.CreateHistogram<double>(name, "s", description);
#endif
        }

        /// <summary>
        /// Gets whether something listens to the database operations, so that they are worth timing.
        /// </summary>
        internal static bool IsEnabled =>
            YesSqlTelemetry.ActivitySource.HasListeners() ||
            _operationDuration.Enabled ||
            YesSqlTelemetryScope.Current != null;

        /// <summary>
        /// Runs a database operation and records it.
        /// </summary>
        public static Task<TResult> ExecuteAsync<TResult, TState>(in DbOperation operation, TState state, Func<TState, Task<TResult>> execute)
        {
            if (!IsEnabled)
            {
                CountDisabled(operation);

                return execute(state);
            }

            return ExecuteInstrumentedAsync(operation, state, execute);
        }

        /// <summary>
        /// Runs a statement through Dapper and records it.
        /// </summary>
        public static Task<int> ExecuteAsync(in DbOperation operation, CommandDefinition command)
            => ExecuteAsync(operation, (operation.Connection, command), static state => state.Connection.ExecuteAsync(state.command));

        // The activity has to be started from here: an asynchronous method restores Activity.Current when it returns.
        private static async Task<TResult> ExecuteInstrumentedAsync<TResult, TState>(DbOperation operation, TState state, Func<TState, Task<TResult>> execute)
        {
            var scope = Start(operation);

            try
            {
                var result = await execute(state);

                scope.Stop();

                return result;
            }
            catch (Exception exception)
            {
                scope.Stop(exception);

                throw;
            }
        }

        /// <summary>
        /// Starts recording a database operation, for the callers which can't hand over its execution. As it changes
        /// <see cref="Activity.Current"/>, it has to be stopped from the asynchronous method that started it.
        /// </summary>
        public static DbOperationScope Start(in DbOperation operation)
        {
            if (!IsEnabled)
            {
                CountDisabled(operation);

                return default;
            }

            var configuration = operation.Configuration;

            if (configuration is null)
            {
                return default;
            }

            var database = operation.Connection.Database;
            var telemetry = GetOptions(configuration);
            var systemName = configuration.SqlDialect.TelemetrySystemName;
            var activity = YesSqlTelemetry.ActivitySource.StartActivity(operation.Name, GetCommandSpanKind(configuration.SqlDialect));

            if (activity is { IsAllDataRequested: true })
            {
                var collection = operation.Table is null ? null : FormatCollection(configuration, operation.Table);
                var target = collection ?? database;

                // The name defined by the OpenTelemetry semantic conventions.
                activity.DisplayName = string.IsNullOrEmpty(target) ? operation.Name : operation.Name + " " + target;

                activity.SetTag(DbSystemName, systemName);
                activity.SetTag(DbOperationName, operation.Name);

                if (!string.IsNullOrEmpty(database))
                {
                    activity.SetTag(DbNamespace, database);
                }

                if (collection is not null)
                {
                    activity.SetTag(DbCollectionName, collection);
                }

                if (operation.BatchSize > 0)
                {
                    activity.SetTag(DbOperationBatchSize, operation.BatchSize);
                }

                if (telemetry.IncludeQueryText && operation.Sql is not null)
                {
                    activity.SetTag(DbQueryText, operation.Sql);
                }

                AddTags(activity, telemetry);
            }

            return new DbOperationScope(activity, operation, systemName, database, Stopwatch.GetTimestamp());
        }

        /// <summary>
        /// Starts recording a flush. As it changes <see cref="Activity.Current"/>, it has to be stopped from the
        /// asynchronous method that started it.
        /// </summary>
        public static FlushScope StartFlush(IConfiguration configuration, YesSqlStatistics statistics)
        {
            YesSqlStatistics.Add(statistics, Counter.Flushes);

            if (!YesSqlTelemetry.ActivitySource.HasListeners() && !_flushDuration.Enabled)
            {
                return default;
            }

            var activity = YesSqlTelemetry.ActivitySource.StartActivity(FlushSpanName, ActivityKind.Internal);

            if (activity is { IsAllDataRequested: true })
            {
                activity.SetTag(DbSystemName, configuration.SqlDialect.TelemetrySystemName);
                AddTags(activity, GetOptions(configuration));
            }

            return new FlushScope(activity, configuration, statistics, Stopwatch.GetTimestamp());
        }

        /// <summary>
        /// Records what a session did, once it has ended.
        /// </summary>
        public static void RecordSession(IConfiguration configuration, YesSqlStatistics statistics)
        {
            if (!_sessionCommands.Enabled && !_sessionDocuments.Enabled)
            {
                return;
            }

            var tags = CreateTags(configuration);

            // A session doing nothing, which can be common, says nothing about the database work of an application.
            if (statistics.Commands > 0)
            {
                _sessionCommands.Record(statistics.Commands, tags);
            }

            RecordDocuments("read", statistics.DocumentsRead, tags);
            RecordDocuments("insert", statistics.DocumentsInserted, tags);
            RecordDocuments("update", statistics.DocumentsUpdated, tags);
            RecordDocuments("delete", statistics.DocumentsDeleted, tags);

            static void RecordDocuments(string operation, long count, TagList tags)
            {
                if (count > 0)
                {
                    tags.Add(DocumentOperation, operation);
                    _sessionDocuments.Record(count, tags);
                }
            }
        }

        /// <summary>
        /// Returns the name of the operation of a batch, as defined by the OpenTelemetry semantic conventions, from its statements.
        /// </summary>
        public static string GetBatchOperationName(IReadOnlyList<string> statements)
        {
            var name = GetStatementOperationName(statements[0]);

            if (statements.Count == 1)
            {
                return name;
            }

            for (var i = 1; i < statements.Count; i++)
            {
                if (!string.Equals(name, GetStatementOperationName(statements[i]), StringComparison.Ordinal))
                {
                    return "BATCH";
                }
            }

            return "BATCH " + name;
        }

        /// <summary>
        /// Returns the first keyword of a statement generated by YesSql, in upper case.
        /// </summary>
        public static string GetStatementOperationName(string statement)
        {
            var span = statement.AsSpan().TrimStart();
            var length = span.IndexOfAny(" \t\r\n");
            var keyword = length < 0 ? span : span[..length];

            foreach (var name in (ReadOnlySpan<string>)[DbOperation.Select, DbOperation.Insert, DbOperation.Update, DbOperation.Delete])
            {
                if (keyword.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }

            return keyword.ToString().ToUpperInvariant();
        }

        private static void CountDisabled(in DbOperation operation)
        {
            if (operation.IsCommand)
            {
                operation.Statistics?.Increment(Counter.Commands);
            }
        }

        // When the spans of the driver are collected too, each database call would otherwise have two nested client spans.
        internal static ActivityKind GetCommandSpanKind(ISqlDialect dialect)
        {
            var driver = dialect.DriverTelemetryName;

            return driver != null && _driverSources.GetOrAdd(driver, static name => new ActivitySource(name)).HasListeners()
                ? ActivityKind.Internal
                : ActivityKind.Client;
        }

        // A custom implementation of IConfiguration may have left them unset.
        private static YesSqlTelemetryOptions GetOptions(IConfiguration configuration)
            => configuration.Telemetry ?? _defaultOptions;

        private static string FormatCollection(IConfiguration configuration, string table)
        {
            var name = configuration.TablePrefix + table;

            return string.IsNullOrEmpty(configuration.Schema) ? name : configuration.Schema + "." + name;
        }

        private static void AddTags(Activity activity, YesSqlTelemetryOptions telemetry)
        {
            for (var i = 0; i < telemetry.Tags.Count; i++)
            {
                activity.SetTag(telemetry.Tags[i].Key, telemetry.Tags[i].Value);
            }
        }

        private static TagList CreateTags(IConfiguration configuration)
        {
            var tags = new TagList
            {
                { DbSystemName, configuration.SqlDialect.TelemetrySystemName },
            };

            var customTags = GetOptions(configuration).Tags;

            for (var i = 0; i < customTags.Count; i++)
            {
                tags.Add(customTags[i]);
            }

            return tags;
        }

        private static void SetError(Activity activity, Exception exception)
        {
            if (activity is not { IsAllDataRequested: true })
            {
                return;
            }

            var type = exception.GetType().FullName;

            activity.SetTag(ErrorType, type);
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.type", type },
                { "exception.message", exception.Message },
                { "exception.stacktrace", exception.ToString() },
            }));
        }

        /// <summary>
        /// A database operation being recorded.
        /// </summary>
        internal struct DbOperationScope
        {
            private readonly Activity _activity;
            private readonly DbOperation _operation;
            private readonly string _systemName;
            private readonly string _database;
            private readonly long _startTimestamp;
            private bool _stopped;

            public DbOperationScope(Activity activity, in DbOperation operation, string systemName, string database, long startTimestamp)
            {
                _activity = activity;
                _operation = operation;
                _systemName = systemName;
                _database = database;
                _startTimestamp = startTimestamp;
            }

            public void Stop(Exception exception = null)
            {
                if (_stopped || _startTimestamp == 0)
                {
                    return;
                }

                _stopped = true;

                var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);

                if (exception is not null)
                {
                    SetError(_activity, exception);
                }

                _activity?.Stop();

                if (_operation.IsCommand)
                {
                    YesSqlStatistics.Add(_operation.Statistics, Counter.Commands);
                    YesSqlStatistics.Add(_operation.Statistics, Counter.CommandDurationTicks, elapsed.Ticks);
                }

                if (_operationDuration.Enabled)
                {
                    // Neither the table nor the SQL text: they would multiply the number of series.
                    var tags = CreateTags(_operation.Configuration);

                    tags.Add(DbOperationName, _operation.Name);

                    if (!string.IsNullOrEmpty(_database))
                    {
                        tags.Add(DbNamespace, _database);
                    }

                    if (exception is not null)
                    {
                        tags.Add(ErrorType, exception.GetType().FullName);
                    }

                    _operationDuration.Record(elapsed.TotalSeconds, tags);
                }
            }
        }

        /// <summary>
        /// A flush being recorded.
        /// </summary>
        internal readonly struct FlushScope
        {
            private readonly Activity _activity;
            private readonly IConfiguration _configuration;
            private readonly YesSqlStatistics _statistics;
            private readonly long _startTimestamp;
            private readonly long _commands;
            private readonly long _inserted;
            private readonly long _updated;
            private readonly long _deleted;

            public FlushScope(Activity activity, IConfiguration configuration, YesSqlStatistics statistics, long startTimestamp)
            {
                _activity = activity;
                _configuration = configuration;
                _statistics = statistics;
                _startTimestamp = startTimestamp;
                _commands = statistics.Commands;
                _inserted = statistics.DocumentsInserted;
                _updated = statistics.DocumentsUpdated;
                _deleted = statistics.DocumentsDeleted;
            }

            public void Stop(Exception exception = null)
            {
                if (_startTimestamp == 0)
                {
                    return;
                }

                var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);

                if (_activity is { IsAllDataRequested: true })
                {
                    _activity.SetTag("yessql.commands", _statistics.Commands - _commands);
                    _activity.SetTag("yessql.documents.inserted", _statistics.DocumentsInserted - _inserted);
                    _activity.SetTag("yessql.documents.updated", _statistics.DocumentsUpdated - _updated);
                    _activity.SetTag("yessql.documents.deleted", _statistics.DocumentsDeleted - _deleted);
                }

                if (exception is not null)
                {
                    SetError(_activity, exception);
                }

                _activity?.Stop();

                if (_flushDuration.Enabled)
                {
                    var tags = CreateTags(_configuration);

                    if (exception is not null)
                    {
                        tags.Add(ErrorType, exception.GetType().FullName);
                    }

                    _flushDuration.Record(elapsed.TotalSeconds, tags);
                }
            }
        }
    }
}
