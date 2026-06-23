using System;
using System.Threading.Tasks;
using YesSql.Filters.Services;

namespace YesSql.Filters.Query.Services
{
    /// <summary>
    /// Represents the options of a query term, including the predicates applied when the term matches.
    /// </summary>
    /// <typeparam name="T">The type of the queried document.</typeparam>
    public class QueryTermOption<T> : TermOption where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryTermOption{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="matchPredicate">The predicate to apply when the term matches.</param>
        public QueryTermOption(string name, Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> matchPredicate) : base(name)
        {
            MatchPredicate = matchPredicate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryTermOption{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the term.</param>
        /// <param name="matchPredicate">The predicate to apply when the term matches.</param>
        /// <param name="notMatchPredicate">The predicate to apply when the term is negated.</param>
        public QueryTermOption(string name, Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> matchPredicate, Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> notMatchPredicate)
            : base(name)
        {
            MatchPredicate = matchPredicate;
            NotMatchPredicate = notMatchPredicate;
        }

        /// <summary>
        /// Gets the predicate to apply when the term matches.
        /// </summary>
        public Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> MatchPredicate { get; }

        /// <summary>
        /// Gets the predicate to apply when the term is negated.
        /// </summary>
        public Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> NotMatchPredicate { get; }
    }
}
