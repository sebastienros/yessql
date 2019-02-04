using System.Text;
using YesSql.Logging;

namespace YesSql.Tests
{
    public class TestLogger : ILogger
    {
        private readonly StringBuilder _builder;

        public TestLogger(StringBuilder builder = null)
        {
            _builder = builder ?? new StringBuilder();
        }

        public bool IsLevelEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log(LogLevel logLevel, string log)
        {
            _builder.AppendLine(logLevel.ToString().ToUpper() + ": " + log);
        }

        public override string ToString()
        {
            return _builder.ToString();
        }
    }
}
