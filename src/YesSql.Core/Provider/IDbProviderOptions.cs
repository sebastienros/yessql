using YesSql.Core.Services;

namespace YesSql.Core.Provider
{
    public interface IDbProviderOptions
    {
        string ProviderName { get; set; }

        Configuration Configuration { get; set; }
    }
}
