using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql.Filters.Abstractions.Nodes;
using YesSql.Filters.Abstractions.Services;
using YesSql.Filters.Query.Services;

namespace YesSql.Filters.Query
{
    public class QueryFilterResult<T> : FilterResult<T, QueryTermOption<T>> where T : class
    {
        public QueryFilterResult(IReadOnlyDictionary<string, QueryTermOption<T>> termOptions) : base(termOptions)
        { }

        public QueryFilterResult(List<TermNode> terms, IReadOnlyDictionary<string, QueryTermOption<T>> termOptions) : base(terms, termOptions)
        { }

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
                    var alwaysRunNode = new NamedTermNode(termOption.Key, new UnaryNode(String.Empty, OperateNodeQuotes.None));
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
