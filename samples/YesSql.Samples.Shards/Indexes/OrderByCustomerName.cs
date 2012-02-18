using YesSql.Core.Indexes;
using YesSql.Samples.Shards.Models;

namespace YesSql.Samples.Shards.Indexes
{
    public class OrderByCustomerName : ReduceIndex
    {
        public virtual string Name { get; set; }
    }

    public class OrderIndexProvider : IndexProvider<Order>
    {
        public override void Describe(DescribeContext<Order> context) 
        {
            context
                .For<OrderByCustomerName, string>()
                .Map(order => new OrderByCustomerName { Name = order.Customer })
                .Group(order => order.Name)
                .Reduce(group => new OrderByCustomerName { Name = group.Key });
        }
        
    }
}
