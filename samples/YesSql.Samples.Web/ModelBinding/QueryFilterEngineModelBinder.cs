using YesSql.Filters.Query;

namespace YesSql.Samples.Web.ModelBinding
{
    public class QueryFilterEngineModelBinder<T> : FilterEngineModelBinder<QueryFilterResult<T>> where T : class
    {
        private readonly IQueryParser<T> _parser;

        public QueryFilterEngineModelBinder(IQueryParser<T> parser)
        {
            _parser = parser;
        }

        public override QueryFilterResult<T> Parse(string text)
            => _parser.Parse(text);
    }
}
