using YesSql.Filters.Abstractions.Services;
using YesSql.Filters.Query.Services;

namespace YesSql.Filters.Query
{
    public interface IQueryParser<T> : IFilterParser<QueryFilterResult<T>> where T : class
    {
    }
}
