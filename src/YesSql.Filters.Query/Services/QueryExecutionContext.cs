using System;
using YesSql.Filters.Abstractions.Services;
using YesSql;

namespace YesSql.Filters.Query.Services
{
    public class QueryExecutionContext<T> : FilterExecutionContext<IQuery<T>> where T : class
    {
        public QueryExecutionContext(IQuery<T> query, IServiceProvider serviceProvider) : base(query, serviceProvider)
        {
        }

        public QueryTermOption<T> CurrentTermOption { get; set; }
    }
}
