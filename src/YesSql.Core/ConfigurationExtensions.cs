using YesSql.Services;

namespace YesSql
{
    public static class ConfigurationExtensions
    {
        public static IConfiguration UseDefaultIdGenerator(this IConfiguration configuration, string tenant = null)
        {
            configuration.IdGenerator = new DefaultIdGenerator(tenant);

            return configuration;
        }

        public static IConfiguration UseBlockIdGenerator(this IConfiguration configuration, int blockSize = 20)
        {
            configuration.IdGenerator = new DbBlockIdGenerator(blockSize);

            return configuration;
        }
    }
}
