using YesSql.Filters.Services;

namespace YesSql.Filters.Query.Services
{
    public class QueryExecutionContext<T> : FilterExecutionContext<IQuery<T>> where T : class
    {
        public QueryExecutionContext(IQuery<T> query) : base(query)
        {
        }

        public QueryTermOption<T> CurrentTermOption { get; set; }
    }
}
