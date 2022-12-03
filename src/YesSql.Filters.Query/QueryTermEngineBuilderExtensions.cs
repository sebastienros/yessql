using System;
using System.Threading.Tasks;
using YesSql.Filters.Abstractions.Builders;
using YesSql.Filters.Query.Services;

namespace YesSql.Filters.Query
{
    public static class QueryTermFilterBuilderExtensions
    {
        /// <summary>
        /// Adds a single condition to a <see cref="TermEngineBuilder{T, TTermOption}"/>.
        /// <param name="builder"></param>.
        /// <param name="matchQuery">The predicate to apply when the term is parsed.</param>
        /// </summary>
        public static QueryUnaryEngineBuilder<T> OneCondition<T>(this TermEngineBuilder<T, QueryTermOption<T>> builder, Func<string, IQuery<T>, IQuery<T>> matchQuery) where T : class
        {
            ValueTask<IQuery<T>> valueQuery(string q, IQuery<T> val, QueryExecutionContext<T> ctx) => new(matchQuery(q, val));

            return builder.OneCondition(valueQuery);
        }

        /// <summary>
        /// Adds a single condition to a <see cref="TermEngineBuilder{T, TTermOption}"/>.
        /// <param name="builder"></param>
        /// <param name="matchQuery">An async predicate to apply when the term is parsed.</param>
        /// </summary>
        public static QueryUnaryEngineBuilder<T> OneCondition<T>(this TermEngineBuilder<T, QueryTermOption<T>> builder, Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> matchQuery) where T : class
        {
            var operatorBuilder = new QueryUnaryEngineBuilder<T>(builder.Name, matchQuery);
            builder.SetOperator(operatorBuilder);

            return operatorBuilder;
        }

        /// <summary>
        /// Adds a condition which supports many operations to a <see cref="TermEngineBuilder{T, TTermOption}"/>
        /// <param name="builder"></param>
        /// <param name="matchQuery">The predicate to apply when the term is parsed with an AND or OR operator.</param>
        /// <param name="notMatchQuery">The predicate to apply when the term is parsed with a NOT operator.</param>
        /// </summary>
        public static QueryBooleanEngineBuilder<T> ManyCondition<T>(
            this TermEngineBuilder<T, QueryTermOption<T>> builder,
            Func<string, IQuery<T>, IQuery<T>> matchQuery,
            Func<string, IQuery<T>, IQuery<T>> notMatchQuery) where T : class
        {
            ValueTask<IQuery<T>> valueMatch(string q, IQuery<T> val, QueryExecutionContext<T> ctx) => new(matchQuery(q, val));
            ValueTask<IQuery<T>> valueNotMatch(string q, IQuery<T> val, QueryExecutionContext<T> ctx) => new(notMatchQuery(q, val));

            return builder.ManyCondition(valueMatch, valueNotMatch);
        }

        /// <summary>
        /// Adds a condition which supports many operations to a <see cref="TermEngineBuilder{T, TTermOption}"/>
        /// <param name="builder"></param>.
        /// <param name="matchQuery">The predicate to apply when the term is parsed with an AND or OR operator.</param>
        /// <param name="notMatchQuery">The predicate to apply when the term is parsed with a NOT operator.</param>
        /// </summary>
        public static QueryBooleanEngineBuilder<T> ManyCondition<T>(
            this TermEngineBuilder<T, QueryTermOption<T>> builder,
            Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> matchQuery,
            Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> notMatchQuery) where T : class
        {
            var operatorBuilder = new QueryBooleanEngineBuilder<T>(builder.Name, matchQuery, notMatchQuery);
            builder.SetOperator(operatorBuilder);

            return operatorBuilder;
        }
    }
}
