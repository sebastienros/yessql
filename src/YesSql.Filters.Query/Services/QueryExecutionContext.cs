using YesSql.Filters.Services;

namespace YesSql.Filters.Query.Services
{
    /// <summary>
    /// Represents the execution context used when applying query filters to an <see cref="IQuery{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the queried document.</typeparam>
    public class QueryExecutionContext<T> : FilterExecutionContext<IQuery<T>> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryExecutionContext{T}"/> class.
        /// </summary>
        /// <param name="query">The query the filters are applied to.</param>
        public QueryExecutionContext(IQuery<T> query) : base(query)
        {
        }

        /// <summary>
        /// Gets or sets the term option currently being executed.
        /// </summary>
        public QueryTermOption<T> CurrentTermOption { get; set; }
    }
}
