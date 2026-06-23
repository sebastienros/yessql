using System.Threading.Tasks;
using YesSql.Filters.Query.Services;

namespace YesSql.Filters.Query
{
    /// <summary>
    /// Provides extension methods to execute a <see cref="QueryFilterResult{T}"/> against an <see cref="IQuery{T}"/>.
    /// </summary>
    public static class QueryFilterResultExtensions
    {
        /// <summary>
        /// Applies the term filters to the specified <see cref="IQuery{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the queried document.</typeparam>
        /// <param name="result">The filter result to execute.</param>
        /// <param name="query">The query to apply the term filters to.</param>
        /// <returns>The filtered <see cref="IQuery{T}"/>.</returns>
        public static ValueTask<IQuery<T>> ExecuteAsync<T>(this QueryFilterResult<T> result, IQuery<T> query) where T : class
            => result.ExecuteAsync(new QueryExecutionContext<T>(query));
    }
}
