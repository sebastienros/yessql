using System;
using System.Threading.Tasks;

namespace YesSql
{
    /// <summary>
    /// Provides methods to configure and create new <see cref="IStore"/> instances.
    /// </summary>
    public class StoreFactory
    {
        /// <summary>
        /// Initializes an <see cref="IStore"/> instance and its new <see cref="Configuration"/>.
        /// </summary>
        /// <param name="config">An action to execute on the <see cref="Configuration"/> of the new <see cref="Store"/> instance.</param>
        public static async Task<IStore> CreateAsync(Action<IConfiguration> configuration)
        {
            var store = new Store(configuration);
            await store.InitializeAsync();
            return store;
        }

        /// <summary>
        /// Initializes an <see cref="IStore"/> instance using a specific <see cref="Configuration"/> instance.
        /// </summary>
        /// <param name="configuration">The <see cref="Configuration"/> instance to use.</param>
        public static async Task<IStore> CreateAsync(IConfiguration configuration)
        {
            var store = new Store(configuration);
            await store.InitializeAsync();
            return store;
        }
    }
}
