using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace YesSql.Tests
{
    public class TestLogger : ILogger
    {
        private object _lockObj = new object();

        private readonly StringBuilder _builder;

        public TestLogger(StringBuilder builder = null)
        {
            _builder = builder ?? new StringBuilder();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new FakeScope();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return TestLogger.IsLevelEnabled(logLevel);
        }

        public static bool IsLevelEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log(LogLevel logLevel, string log)
        {
            lock (_lockObj)
            {
                _builder.AppendLine(logLevel.ToString().ToUpper() + ": " + log);
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Log(logLevel, formatter(state, exception));
        }

        public override string ToString()
        {
            lock (_lockObj)
            {
                return _builder.ToString();
            }
        }
    }

    public sealed class FakeScope : IDisposable
    {
        public void Dispose()
        {

        }
    }
}
