using YesSql.Core.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PersonByAge : MapIndex
    {
        public int Age { get; set; }
        public bool Adult { get; set; }
    }

    public class PersonAgeIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByAge>()
                .Map(person => new PersonByAge { Age = person.Age, Adult = person.Age >= 18 });
        }
    }
}
