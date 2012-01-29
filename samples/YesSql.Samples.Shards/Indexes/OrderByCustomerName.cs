using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Samples.Shards.Models;

namespace YesSql.Samples.Shards.Indexes
{
    public class OrderByCustomerName : HasDocumentsIndex
    {
        [GroupKey]
        public virtual string Name { get; set; }

        public override void Describe(DescribeContext context) {
            context
                .For<Order, OrderByCustomerName, string>()
                .Index(
                    map: orders => orders.Select(p => new OrderByCustomerName { Name = p.Customer }),
                    reduce: group => new OrderByCustomerName { Name = group.Key }
            );
        }
    }
}
