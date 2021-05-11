using System;
using System.Collections.Generic;
using System.Linq;
using YesSql.Filters.Abstractions.Builders;
using YesSql.Filters.Query.Services;

namespace YesSql.Filters.Query
{
    /// <summary>
    /// Builds a <see cref="QueryEngineBuilder{T}"/> for an <see cref="IQuery{T}"/>.
    /// </summary>
    public class QueryEngineBuilder<T> where T : class
    {
        private Dictionary<string, TermEngineBuilder<T, QueryTermOption<T>>> _termBuilders = new Dictionary<string, TermEngineBuilder<T, QueryTermOption<T>>>();

        public QueryEngineBuilder<T> SetTermParser(TermEngineBuilder<T, QueryTermOption<T>> builder)
        {
            _termBuilders[builder.Name] = builder;

            return this;
        }

        public IQueryParser<T> Build()
        {
            var builders = _termBuilders.Values.Select(x => x.Build());

            var parsers = builders.Select(x => x.Parser).ToArray();
            var termOptions = builders.Select(x => x.TermOption).ToDictionary(k => k.Name, v => v, StringComparer.OrdinalIgnoreCase);

            return new QueryParser<T>(parsers, termOptions);
        }
    }
}
