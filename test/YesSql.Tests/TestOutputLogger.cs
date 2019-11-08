using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using System;
using Xunit.Abstractions;

namespace YesSql.Tests
{
    public class TestOutputLogger : ILogger
    {
        private readonly ITestOutputHelper _logger;

        public TestOutputLogger(ITestOutputHelper logger)
        {
            _logger = logger;
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
            _logger.WriteLine(logLevel.ToString().ToUpper() + ": " + log);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Log(logLevel, formatter(state, exception));
        }

    }
}
