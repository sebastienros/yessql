namespace YesSql.Logging
{
    public interface ILogger
    {
        bool IsLevelEnabled(LogLevel logLevel);
        void Log(LogLevel logLevel, string log);
    }
}
