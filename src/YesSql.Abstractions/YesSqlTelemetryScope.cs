using System;
using System.Threading;

namespace YesSql
{
    /// <summary>
    /// Totals the database work of an asynchronous flow. Created by <see cref="YesSqlTelemetry.BeginScope"/>.
    /// </summary>
    public sealed class YesSqlTelemetryScope : IDisposable
    {
        private static readonly AsyncLocal<YesSqlTelemetryScope> _current = new();

        // Prevents an application that doesn't use scopes from reading the AsyncLocal for each command.
        private static volatile bool _used;

        internal YesSqlTelemetryScope()
        {
            _used = true;
            Parent = _current.Value;
            _current.Value = this;
        }

        /// <summary>
        /// Gets the scope of the current asynchronous flow, or <see langword="null"/> if there is none.
        /// </summary>
        public static YesSqlTelemetryScope Current => _used ? _current.Value : null;

        /// <summary>
        /// Gets the totals of this scope. They can still be read once it is disposed.
        /// </summary>
        public YesSqlStatistics Statistics { get; } = new();

        internal YesSqlTelemetryScope Parent { get; }

        /// <summary>
        /// Ends the scope: the following work isn't counted by it anymore.
        /// </summary>
        public void Dispose()
        {
            if (_current.Value == this)
            {
                _current.Value = Parent;
            }
        }
    }
}
