using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace YesSql
{
    /// <summary>
    /// The telemetry emitted by YesSql, and the names to subscribe to in order to collect it.
    /// </summary>
    /// <remarks>
    /// YesSql emits through <see cref="System.Diagnostics"/> only. Nothing is recorded until a listener, such as the
    /// OpenTelemetry SDK, subscribes to <see cref="Name"/>.
    /// </remarks>
    public static class YesSqlTelemetry
    {
        /// <summary>
        /// The name of both the <see cref="ActivitySource"/> and the <see cref="Meter"/> of YesSql.
        /// </summary>
        public const string Name = "YesSql";

        private static readonly string _version = typeof(YesSqlTelemetry).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        /// <summary>
        /// Gets the source of the spans of YesSql.
        /// </summary>
        public static ActivitySource ActivitySource { get; } = new(Name, _version);

        /// <summary>
        /// Gets the meter of the instruments of YesSql.
        /// </summary>
        public static Meter Meter { get; } = new(Name, _version);

        /// <summary>
        /// Gets the names of the <see cref="ActivitySource"/> of the database drivers used by the YesSql providers.
        /// </summary>
        /// <remarks>
        /// Subscribing to them is optional: YesSql already emits a span for each of its commands. A driver span adds the
        /// details of the connection, nested in the YesSql span, which then changes its kind from
        /// <see cref="ActivityKind.Client"/> to <see cref="ActivityKind.Internal"/> so that a single span of a database
        /// call stays a client one. A name whose driver isn't loaded is simply ignored.
        /// </remarks>
        public static IReadOnlyList<string> DriverActivitySources { get; } = ["Npgsql", "MySqlConnector"];

        /// <summary>
        /// Gets the names of the <see cref="Meter"/> of the database drivers used by the YesSql providers.
        /// </summary>
        /// <remarks>
        /// They report what YesSql can't see, such as the state of the connection pools. A name whose driver isn't loaded
        /// is simply ignored. The other drivers don't publish a meter.
        /// </remarks>
        public static IReadOnlyList<string> DriverMeters { get; } = ["Npgsql", "MySqlConnector"];

        /// <summary>
        /// Starts totalling the database work of the current asynchronous flow, and of the flows it starts, whatever the
        /// sessions doing it. Dispose the returned scope to end it.
        /// </summary>
        /// <remarks>
        /// Use it to report the totals of a unit of work spanning several sessions, such as an HTTP request. Scopes can be
        /// nested: the work is counted by each of them. The work of a single session is available from
        /// <see cref="ISession.Statistics"/> without a scope.
        /// </remarks>
        public static YesSqlTelemetryScope BeginScope() => new();
    }
}
