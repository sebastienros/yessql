using YesSql.Core.Services;

namespace YesSql.Core.Provider
{
    public class DbProviderOptions : IDbProviderOptions
    {
        public string ProviderName { get; set; }
        public Configuration Configuration { get; set; }
    }
}
