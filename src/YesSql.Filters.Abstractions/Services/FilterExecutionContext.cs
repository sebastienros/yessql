namespace YesSql.Filters.Services
{
    public abstract class FilterExecutionContext<T>
    {
        protected FilterExecutionContext(T item)
        {
            Item = item;
        }

        public T Item { get; set; }
    }
}
