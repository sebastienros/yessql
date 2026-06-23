using System;
using YesSql.Filters.Builders;
using YesSql.Filters.Query.Services;

namespace YesSql.Filters.Query
{
    public static class QueryEngineBuilderExtensions
    {
        /// <summary>
        /// Adds a term where the name must be specified to an <see cref="QueryEngineBuilder{T}"/>
        /// </summary>
        public static QueryEngineBuilder<T> WithNamedTerm<T>(this QueryEngineBuilder<T> builder, string name, Action<NamedTermEngineBuilder<T, QueryTermOption<T>>> action) where T : class
        {
            var parserBuilder = new NamedTermEngineBuilder<T, QueryTermOption<T>>(name);
            action(parserBuilder);

            builder.SetTermParser(parserBuilder);
            return builder;
        }

        /// <summary>
        /// Adds a term where the name is optional to an <see cref="QueryEngineBuilder{T}"/>
        /// </summary>
        public static QueryEngineBuilder<T> WithDefaultTerm<T>(this QueryEngineBuilder<T> builder, string name, Action<DefaultTermEngineBuilder<T, QueryTermOption<T>>> action) where T : class
        {
            var parserBuilder = new DefaultTermEngineBuilder<T, QueryTermOption<T>>(name);
            action(parserBuilder);

            builder.SetTermParser(parserBuilder);
            return builder;
        }
    }
}
