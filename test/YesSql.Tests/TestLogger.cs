using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using System;
using System.Text;

namespace YesSql.Tests
{
    public class TestLogger : ILogger
    {
        private readonly StringBuilder _builder;

        public TestLogger(StringBuilder builder = null)
        {
            _builder = builder ?? new StringBuilder();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return IsLevelEnabled(logLevel);
        }

        public bool IsLevelEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log(LogLevel logLevel, string log)
        {
            _builder.AppendLine(logLevel.ToString().ToUpper() + ": " + log);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Log(logLevel, formatter(state, exception));
        }

        public override string ToString()
        {
            return _builder.ToString();
        }
    }
}
