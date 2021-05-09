using System.Threading.Tasks;
using YesSql.Filters.Query.Services;

namespace YesSql.Filters.Query
{
    public static class QueryFilterResultExtensions
    {
        public static ValueTask<IQuery<T>> ExecuteAsync<T>(this QueryFilterResult<T> result, IQuery<T> query) where T : class
            => result.ExecuteAsync(new QueryExecutionContext<T>(query));
    }
}
