namespace YesSql.Filters.Abstractions.Services
{
    public abstract class FilterExecutionContext<T>
    {
        public FilterExecutionContext(T item)
        {
            Item = item;
        }

        public T Item { get; set; }
    }
}
