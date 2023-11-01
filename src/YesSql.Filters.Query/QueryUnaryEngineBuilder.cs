using System;
using System.Threading.Tasks;
using YesSql.Filters.Builders;
using YesSql.Filters.Nodes;
using YesSql.Filters.Services;
using YesSql.Filters.Query.Services;

namespace YesSql.Filters.Query
{

    public class QueryUnaryEngineBuilder<T> : UnaryEngineBuilder<T, QueryTermOption<T>> where T : class
    {
        public QueryUnaryEngineBuilder(string name, Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> query) : base(new QueryTermOption<T>(name, query))
        {
        }

        /// <summary>
        /// Adds a mapping function which can be applied to a model.
        /// <typeparam name="TModel">The type of model.</typeparam>
        /// </summary>
        public QueryUnaryEngineBuilder<T> MapTo<TModel>(Action<string, TModel> map)
        {
            _termOption.MapTo = map;

            return this;
        }

        /// <summary>
        /// Adds a mapping function where terms can be mapped from a model.
        /// <typeparam name="TModel">The type of model.</typeparam>
        /// <param name="map">Mapping to apply</param>
        /// </summary>
        public QueryUnaryEngineBuilder<T> MapFrom<TModel>(Func<TModel, (bool, string)> map)
        {
            static TermNode factory(string name, string value) => new NamedTermNode(name, new UnaryNode(value, OperateNodeQuotes.None));

            return MapFrom(map, factory);
        }

        /// <summary>
        /// Adds a mapping function where terms can be mapped from a model.
        /// <typeparam name="TModel">The type of model.</typeparam>
        /// <param name="map">Mapping to apply</param>
        /// <param name="factory">Factory to create a <see cref="TermNode" /> when adding a mapping</param>
        /// </summary>
        public QueryUnaryEngineBuilder<T> MapFrom<TModel>(Func<TModel, (bool, string)> map, Func<string, string, TermNode> factory)
        {
            Action<QueryFilterResult<T>, string, TermOption, TModel> mapFrom = (QueryFilterResult<T> terms, string name, TermOption termOption, TModel model) =>
            {
                (var shouldMap, var value) = map(model);
                if (shouldMap)
                {
                    var node = termOption.MapFromFactory(name, value);
                    terms.TryAddOrReplace(node);
                }                
                else
                {
                    terms.TryRemove(name);
                }
            };

            _termOption.MapFrom = mapFrom;
            _termOption.MapFromFactory = factory;

            return this;
        }
    }
}
