using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Samples.Shards.Models;

namespace YesSql.Samples.Shards.Indexes
{
    public class OrderByCustomerName : ReduceIndex
    {
        [GroupKey]
        public virtual string Name { get; set; }
    }

    public class OrderIndexProvider : IndexProvider<Order>
    {
        public override void Describe(DescribeContext<Order> context) 
        {
            context
                .For<OrderByCustomerName, string>()
                .Index(
                    map: orders => orders.Select(p => new OrderByCustomerName { Name = p.Customer }),
                    reduce: group => new OrderByCustomerName { Name = group.Key }
            );
        }
        
    }
}
