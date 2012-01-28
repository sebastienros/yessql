using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Shards.Demo.Models;

namespace YesSql.Shards.Demo.Indexes
{
    public class OrderByCustomerName : HasDocumentsIndex, IIndexProvider
    {
        [GroupKey]
        public virtual string Name { get; set; }

        public virtual void Describe(DescribeContext context) {
            context
                .For<Order, OrderByCustomerName, string>()
                .Index(
                    map: orders => orders.Select(p => new OrderByCustomerName { Name = p.Customer }),
                    reduce: group => new OrderByCustomerName { Name = group.Key }
            );
        }
    }
}
