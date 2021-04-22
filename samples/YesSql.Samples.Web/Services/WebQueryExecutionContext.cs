using System;
using YesSql.Filters.Query.Services;

namespace YesSql.Samples.Web.Services
{
    public class WebQueryExecutionContext<T> : QueryExecutionContext<T> where T : class
    {
        public WebQueryExecutionContext(IServiceProvider serviceProvider, IQuery<T> query) : base(query)
        {
        }
    }
}
