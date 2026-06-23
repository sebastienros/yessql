using System;
using System.Threading.Tasks;
using YesSql.Filters.Builders;
using YesSql.Filters.Query.Services;

namespace YesSql.Filters.Query
{
    /// <summary>
    /// Builds a term that supports many operations (AND, OR and NOT) for an <see cref="IQuery{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the queried document.</typeparam>
    public class QueryBooleanEngineBuilder<T> : BooleanEngineBuilder<T, QueryTermOption<T>> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryBooleanEngineBuilder{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="matchQuery">The predicate to apply when the term is parsed with an AND or OR operator.</param>
        /// <param name="notMatchQuery">The predicate to apply when the term is parsed with a NOT operator.</param>
        public QueryBooleanEngineBuilder(
            string name,
            Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> matchQuery,
            Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> notMatchQuery)
        {
            _termOption = new QueryTermOption<T>(name, matchQuery, notMatchQuery);
        }
    }
}
