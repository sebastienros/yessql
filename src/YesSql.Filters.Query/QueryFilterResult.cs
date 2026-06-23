using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Filters.Nodes;
using YesSql.Filters.Services;
using YesSql.Filters.Query.Services;

namespace YesSql.Filters.Query
{
    /// <summary>
    /// Represents the result of parsing a query filter that can be applied to an <see cref="IQuery{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the queried document.</typeparam>
    public class QueryFilterResult<T> : FilterResult<T, QueryTermOption<T>> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFilterResult{T}"/> class.
        /// </summary>
        /// <param name="termOptions">The available term options.</param>
        public QueryFilterResult(IReadOnlyDictionary<string, QueryTermOption<T>> termOptions) : base(termOptions)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFilterResult{T}"/> class.
        /// </summary>
        /// <param name="terms">The parsed terms.</param>
        /// <param name="termOptions">The available term options.</param>
        public QueryFilterResult(IReadOnlyList<TermNode> terms, IReadOnlyDictionary<string, QueryTermOption<T>> termOptions) : base(terms, termOptions)
        { }

        /// <summary>
        /// Maps the terms from the specified model.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to map from.</typeparam>
        /// <param name="model">The model to map the terms from.</param>
        public void MapFrom<TModel>(TModel model)
        {
            foreach (var option in TermOptions)
            {
                if (option.Value.MapFrom is Action<QueryFilterResult<T>, string, TermOption, TModel> mappingMethod)
                {
                    mappingMethod(this, option.Key, option.Value, model);
                }
            }
        }

        /// <summary>
        /// Applies term filters to an <see cref="IQuery{T}"/>
        /// </summary>
        public async ValueTask<IQuery<T>> ExecuteAsync(QueryExecutionContext<T> context)
        {
            var visitor = new QueryFilterVisitor<T>();

            foreach (var term in _terms.Values)
            {
                // TODO optimize value task.
                await VisitTerm(TermOptions, context, visitor, term);
            }

            // Execute always run terms. These are not added to the terms list.
            foreach (var termOption in TermOptions)
            {
                if (!termOption.Value.AlwaysRun)
                {
                    continue;
                }

                if (!_terms.ContainsKey(termOption.Key))
                {
                    var alwaysRunNode = new NamedTermNode(termOption.Key, new UnaryNode(string.Empty, OperateNodeQuotes.None));
                    await VisitTerm(TermOptions, context, visitor, alwaysRunNode);
                }
            }

            return context.Item;
        }

        private async static Task VisitTerm(IReadOnlyDictionary<string, QueryTermOption<T>> termOptions, QueryExecutionContext<T> context, QueryFilterVisitor<T> visitor, TermNode term)
        {
            context.CurrentTermOption = termOptions[term.TermName];

            var termQuery = visitor.Visit(term, context);
            context.Item = await termQuery.Invoke(context.Item);
            context.CurrentTermOption = null;
        }
    }
}
