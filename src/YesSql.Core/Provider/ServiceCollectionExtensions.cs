using System;
using YesSql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDbProvider(
            this IServiceCollection services,
            Action<IConfiguration> setupAction)
        {
            ArgumentNullException.ThrowIfNull(services);

            ArgumentNullException.ThrowIfNull(setupAction);

            var config = new Configuration();
            setupAction.Invoke(config);
            services.AddSingleton<IStore>(StoreFactory.CreateAndInitializeAsync(config).GetAwaiter().GetResult());

            return services;
        }
    }
}