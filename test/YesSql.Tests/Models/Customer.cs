namespace YesSql.Tests.Models
{
    public class Customer
    {
        public string Name { get; set; }
        public long CustomerId { get; set; }
        public long? OrderId { get; set; }
        public bool Active { get; set; }
        public int Version { get; set; }
    }
}
