using System;
using System.Collections.Generic;
using System.Linq;
using YesSql.Filters.Builders;
using YesSql.Filters.Query.Services;

namespace YesSql.Filters.Query
{
    /// <summary>
    /// Builds a <see cref="QueryEngineBuilder{T}"/> for an <see cref="IQuery{T}"/>.
    /// </summary>
    public class QueryEngineBuilder<T> where T : class
    {
        private readonly Dictionary<string, TermEngineBuilder<T, QueryTermOption<T>>> _termBuilders = [];

        /// <summary>
        /// Registers a term parser on the builder.
        /// </summary>
        /// <param name="builder">The term parser builder to register.</param>
        /// <returns>The <see cref="QueryEngineBuilder{T}"/> instance to allow chaining.</returns>
        public QueryEngineBuilder<T> SetTermParser(TermEngineBuilder<T, QueryTermOption<T>> builder)
        {
            _termBuilders[builder.Name] = builder;

            return this;
        }

        /// <summary>
        /// Builds the <see cref="IQueryParser{T}"/> from the registered terms.
        /// </summary>
        /// <returns>A new <see cref="IQueryParser{T}"/> instance.</returns>
        public IQueryParser<T> Build()
        {
            var builders = _termBuilders.Values.Select(x => x.Build());

            var parsers = builders.Select(x => x.Parser).ToArray();
            var termOptions = builders.Select(x => x.TermOption).ToDictionary(k => k.Name, v => v, StringComparer.OrdinalIgnoreCase);

            return new QueryParser<T>(parsers, termOptions);
        }
    }
}
