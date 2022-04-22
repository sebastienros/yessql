using System;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class CustomerByCreationDate : MapIndex
    {
        public long CustomerId { get; set; }
        public long? OrderId { get; set; }
        public bool Active { get; set; }
        public int Version { get; set; }
        public DateTime? CreationDate { get; set; }

    }
    public class CustomerByCreationDateProvider : IndexProvider<Customer>
    {
        public override void Describe(DescribeContext<Customer> context)
        {
            context.For<CustomerByCreationDate>()
                .Map(customer => new CustomerByCreationDate
                {
                    CustomerId = customer.CustomerId,
                    OrderId = customer.OrderId,
                    Active=customer.Active,
                    Version=customer.Version,
                    CreationDate = customer.CreationDate
                });
        }
    }
}
