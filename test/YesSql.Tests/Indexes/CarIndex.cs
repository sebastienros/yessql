using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class CarIndex : MapIndex
    {
        public string Name { get; set; }
        public Categories Category { get; set; }
    }

    public class CarIndexProvider : IndexProvider<Car>
    {
        public override void Describe(DescribeContext<Car> context)
        {
            context
                .For<CarIndex>()
                .Map(c => new CarIndex
                {
                    Name = c.Name,
                    Category = c.Category,
                });
        }
    }
}
