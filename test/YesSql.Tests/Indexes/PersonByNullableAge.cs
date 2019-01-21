using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PersonByNullableAge : MapIndex
    {
        public int? Age { get; set; }
    }

    public class PersonByNullableAgeIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByNullableAge>()
                .Map(person => new PersonByNullableAge
                {
                    Age = person.Age > 99 || person.Age < 0 ? default(int?) : person.Age,
                });
        }
    }
}
