namespace YesSql.Filters.Services
{
    /// <summary>
    /// Represents the context in which a filter term is executed against a single item.
    /// </summary>
    /// <typeparam name="T">The type of the item being filtered.</typeparam>
    public abstract class FilterExecutionContext<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterExecutionContext{T}"/> class.
        /// </summary>
        /// <param name="item">The item the filter is executed against.</param>
        protected FilterExecutionContext(T item)
        {
            Item = item;
        }

        /// <summary>
        /// Gets or sets the item the filter is executed against.
        /// </summary>
        public T Item { get; set; }
    }
}
