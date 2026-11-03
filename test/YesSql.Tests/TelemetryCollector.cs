using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;

namespace YesSql.Tests
{
    /// <summary>
    /// Collects the telemetry of the current test. All its work runs in a dedicated trace, which keeps out the telemetry
    /// of the other tests running in parallel, as the source and the meter of YesSql are shared by the whole process.
    /// </summary>
    public sealed class TelemetryCollector : IDisposable
    {
        private const string TestSourceName = "YesSql.Tests";

        private static readonly ActivitySource _testSource = new(TestSourceName);

        private readonly ActivityListener _activityListener;
        private readonly MeterListener _meterListener;
        private readonly Activity _root;
        private readonly ConcurrentQueue<Activity> _spans = new();
        private readonly ConcurrentQueue<TelemetryMeasurement> _measurements = new();
        private readonly ConcurrentQueue<Instrument> _instruments = new();

        /// <param name="driverNames">The names of the sources and meters of the database drivers to subscribe to as well.</param>
        public TelemetryCollector(params string[] driverNames)
        {
            var names = new HashSet<string>(driverNames) { YesSqlTelemetry.Name };

            _activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == TestSourceName || names.Contains(source.Name),
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    if (activity != _root && activity.TraceId == _root?.TraceId)
                    {
                        _spans.Enqueue(activity);
                    }
                },
            };

            ActivitySource.AddActivityListener(_activityListener);

            _root = _testSource.StartActivity("test");

            _meterListener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (names.Contains(instrument.Meter.Name))
                    {
                        _instruments.Enqueue(instrument);
                        listener.EnableMeasurementEvents(instrument);
                    }
                },
            };

            _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
            _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
            _meterListener.SetMeasurementEventCallback<int>((instrument, value, tags, _) => Record(instrument, value, tags));
            _meterListener.Start();
        }

        /// <summary>
        /// Gets the stopped spans of this test.
        /// </summary>
        public IReadOnlyList<Activity> Spans => _spans.ToArray();

        /// <summary>
        /// Gets the stopped spans of YesSql about database operations.
        /// </summary>
        public IReadOnlyList<Activity> DbSpans => Spans
            .Where(x => x.Source.Name == YesSqlTelemetry.Name && x.GetTagItem("db.operation.name") != null)
            .ToArray();

        /// <summary>
        /// Gets the measurements of this test.
        /// </summary>
        public IReadOnlyList<TelemetryMeasurement> Measurements => _measurements.ToArray();

        /// <summary>
        /// Gets the published instruments of the meters this collector subscribes to.
        /// </summary>
        public IReadOnlyList<Instrument> Instruments => _instruments.ToArray();

        public void Dispose()
        {
            _root?.Stop();
            _meterListener.Dispose();
            _activityListener.Dispose();
        }

        private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object>> tags)
        {
            if (Activity.Current?.TraceId == _root?.TraceId && _root != null)
            {
                _measurements.Enqueue(new TelemetryMeasurement(instrument.Meter.Name, instrument.Name, value, tags.ToArray().ToDictionary(x => x.Key, x => x.Value)));
            }
        }
    }

    public sealed record TelemetryMeasurement(string Meter, string Instrument, double Value, Dictionary<string, object> Tags);
}
