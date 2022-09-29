using System.Collections.Generic;

namespace YesSql.Tests.Models
{
    public class Product
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal Cost { get; set; }
    }

    public class Order
    {
        public long Id { get; set; }
        public string Customer { get; set; }
        public IList<OrderLine> OrderLines { get; set; }

        public Order()
        {
            OrderLines = new List<OrderLine>();
        }
    }

    public class OrderLine
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
