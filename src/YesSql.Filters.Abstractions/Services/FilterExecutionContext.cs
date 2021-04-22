using System;

namespace YesSql.Filters.Abstractions.Services
{
    public class FilterExecutionContext<T>
    {
        // TODO remove service provider. Can be added to custom context.
        public FilterExecutionContext(T item, IServiceProvider serviceProvider)
        {
            Item = item;
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }
        public T Item { get; }
    }
}
