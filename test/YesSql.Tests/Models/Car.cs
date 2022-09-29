namespace YesSql.Tests.Models
{
    public class Car
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public Categories Category { get; set; }
    }

    public enum Categories
    {
        Van,
        Truck
    }
}
