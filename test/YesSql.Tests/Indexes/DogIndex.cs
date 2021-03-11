using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class DogIndex : AnimalIndex
    {
        public string Breed { get; set; }
    }

    public class DogIndexProvider : IndexProvider<Dog>
    {
        public override void Describe(DescribeContext<Dog> context)
        {
            context
                .For<DogIndex>()
                .Map(dog => new DogIndex
                {
                    Name = dog.Name,
                    Breed = dog.Breed
                });
        }
    }
}
