using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class CarIntIndex : MapIndex
    {
        public string Name { get; set; }
        public Categories Category { get; set; }
    }

    public class CarIntIndexProvider : IndexProvider<Car>
    {
        public override void Describe(DescribeContext<Car> context)
        {
            context
                .For<CarIntIndex>()
                .Map(c => new CarIntIndex
                {
                    Name = c.Name,
                    Category = c.Category,
                });
        }
    }
}
