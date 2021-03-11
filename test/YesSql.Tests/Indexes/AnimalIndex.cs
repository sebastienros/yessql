using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class AnimalIndex : MapIndex
    {
        public string Name { get; set; }
    }

    public class AnimalIndexProvider : IndexProvider<Animal>
    {
        public override void Describe(DescribeContext<Animal> context)
        {
            context
                .For<AnimalIndex>()
                .Map(animal => new AnimalIndex
                {
                    Name = animal.Name
                });
        }
    }
}
