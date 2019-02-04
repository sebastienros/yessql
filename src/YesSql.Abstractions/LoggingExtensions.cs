using System;
using YesSql.Logging;

namespace YesSql
{
    public static class ILoggerExtensions
    {
        public static bool LogDebug(this ILogger logger, string log)
        {
            if (logger.IsLevelEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, log);
                return true;
            }

            return false;
        }
        public static bool LogDebug(this ILogger logger, Func<string> log)
        {
            if (logger.IsLevelEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, log());
                return true;
            }

            return false;
        }

        public static bool LogSql(this ILogger logger, string log)
        {
            if (logger.IsLevelEnabled(LogLevel.Sql))
            {
                logger.Log(LogLevel.Sql, log);
                return true;
            }

            return false;
        }
        public static bool LogSql(this ILogger logger, Func<string> log)
        {
            if (logger.IsLevelEnabled(LogLevel.Sql))
            {
                logger.Log(LogLevel.Sql, log());
                return true;
            }

            return false;
        }

        public static bool LogInformation(this ILogger logger, string log)
        {
            if (logger.IsLevelEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, log);
                return true;
            }

            return false;
        }
        public static bool LogInformation(this ILogger logger, Func<string> log)
        {
            if (logger.IsLevelEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, log());
                return true;
            }

            return false;
        }

        public static bool LogWarning(this ILogger logger, string log)
        {
            if (logger.IsLevelEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, log);
                return true;
            }

            return false;
        }
        public static bool LogWarning(this ILogger logger, Func<string> log)
        {
            if (logger.IsLevelEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, log());
                return true;
            }

            return false;
        }

        public static bool LogError(this ILogger logger, string log)
        {
            if (logger.IsLevelEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, log);
                return true;
            }

            return false;
        }

        public static bool LogError(this ILogger logger, Func<string> log)
        {
            if (logger.IsLevelEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, log());
                return true;
            }

            return false;
        }
    }
}
