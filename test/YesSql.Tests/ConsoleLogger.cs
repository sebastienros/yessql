using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace YesSql.Tests
{
    public class ConsoleLogger : ILogger
    {
        private readonly ITestOutputHelper _output;

        public ConsoleLogger(ITestOutputHelper output)
        {
            _output = output;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new FakeScope();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return ConsoleLogger.IsLevelEnabled(logLevel);
        }

        public static bool IsLevelEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log(LogLevel logLevel, string log)
        {
            _output.WriteLine(logLevel.ToString().ToUpper() + ": " + log);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Log(logLevel, formatter(state, exception));
        }
    }
}
