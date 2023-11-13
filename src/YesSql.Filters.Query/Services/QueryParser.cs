using Parlot;
using Parlot.Fluent;
using System.Collections.Generic;
using YesSql.Filters.Abstractions.Nodes;
using static Parlot.Fluent.Parsers;

namespace YesSql.Filters.Query.Services
{
    public class QueryParser<T> : IQueryParser<T> where T : class
    {
        private readonly Dictionary<string, QueryTermOption<T>> _termOptions;
        private readonly Parser<QueryFilterResult<T>> _parser;

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
