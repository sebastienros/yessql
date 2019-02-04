namespace YesSql.Logging
{
    public class NullLogger : ILogger
    {
        public static NullLogger Instance = new NullLogger();

        public bool IsLevelEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log(LogLevel logLevel, string log)
        {
            return;
        }
    }
}
