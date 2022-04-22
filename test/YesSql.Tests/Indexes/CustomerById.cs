using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class CustomerById : MapIndex
    {
        public long CustomerId { get; set; }
        public long? OrderId { get; set; }
        public bool Active { get; set; }
        public int Version { get; set; }

    }
    public class CustomerByIdProvider : IndexProvider<Customer>
    {
        public override void Describe(DescribeContext<Customer> context)
        {
            context.For<CustomerById>()
                .Map(customer => new CustomerById
                {
                    CustomerId = customer.CustomerId,
                    OrderId = customer.OrderId,
                    Active=customer.Active,
                    Version=customer.Version
                });
        }
    }
}
