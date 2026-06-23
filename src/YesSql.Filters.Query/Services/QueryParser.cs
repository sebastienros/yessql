using Parlot;
using Parlot.Fluent;
using System.Collections.Generic;
using YesSql.Filters.Nodes;
using static Parlot.Fluent.Parsers;

namespace YesSql.Filters.Query.Services
{
    /// <summary>
    /// Parses a query filter string into a <see cref="QueryFilterResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the queried document.</typeparam>
    public class QueryParser<T> : IQueryParser<T> where T : class
    {
        private readonly Dictionary<string, QueryTermOption<T>> _termOptions;
        private readonly Parser<QueryFilterResult<T>> _parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParser{T}"/> class.
        /// </summary>
        /// <param name="termParsers">The term parsers used to parse each term.</param>
        /// <param name="termOptions">The available term options.</param>
        public QueryParser(Parser<TermNode>[] termParsers, Dictionary<string, QueryTermOption<T>> termOptions)
        {
            _termOptions = termOptions;

            var terms = OneOf(termParsers);

            _parser = ZeroOrMany(terms)
                    .Then(static (context, terms) =>
                    {
                        var ctx = (QueryParseContext<T>)context;

                        return new QueryFilterResult<T>(terms, ctx.TermOptions);
                    }).Compile();
        }

        /// <summary>
        /// Parses the specified text into a <see cref="QueryFilterResult{T}"/>.
        /// </summary>
        /// <param name="text">The query filter text to parse.</param>
        /// <returns>The <see cref="QueryFilterResult{T}"/> produced from the parsed text.</returns>
        public QueryFilterResult<T> Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new QueryFilterResult<T>(_termOptions);
            }

            var context = new QueryParseContext<T>(_termOptions, new Scanner(text));

            var result = default(ParseResult<QueryFilterResult<T>>);
            if (_parser.Parse(context, ref result))
            {
                return result.Value;
            }

            return new QueryFilterResult<T>(_termOptions);
        }
    }
}
