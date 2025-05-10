using YesSql.Services;

namespace YesSql
{
    public static class ConfigurationExtensions
    {
        public static IConfiguration UseDefaultIdGenerator(this IConfiguration configuration)
        {
            configuration.IdGenerator = new DefaultIdGenerator();

            return configuration;
        }

        public static IConfiguration UseBlockIdGenerator(this IConfiguration configuration, int blockSize = 20)
        {
            configuration.IdGenerator = new DbBlockIdGenerator(blockSize);

            return configuration;
        }

        public static IConfiguration UseThreadSafetyChecks(this IConfiguration configuration, bool enable = true)
        {
            configuration.EnableThreadSafetyChecks = enable;

            return configuration;
        }
    }
}
