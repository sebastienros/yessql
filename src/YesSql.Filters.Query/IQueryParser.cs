using YesSql.Filters.Services;

namespace YesSql.Filters.Query
{
    /// <summary>
    /// Represents a filter parser for an <see cref="IQuery{T}"/>
    /// </summary>
    public interface IQueryParser<T> : IFilterParser<QueryFilterResult<T>> where T : class
    {
    }
}
