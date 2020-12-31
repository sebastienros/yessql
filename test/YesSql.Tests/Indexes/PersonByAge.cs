using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PersonByAge : MapIndex
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public bool Adult { get; set; }
    }

    public class PersonAgeIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByAge>()
                .Map(person => new PersonByAge
                {
                    Age = person.Age,
                    Adult = person.Age >= 18,
                    Name = person.Firstname
                });
        }
    }

    public class PersonAgeIndexFilterProvider1 : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByAge>()
                .When(x => x.Age != 0)
                .Map(person => new PersonByAge
                {
                    Age = person.Age,
                    Adult = person.Age >= 18,
                    Name = person.Firstname
                });
        }
    }

    public class PersonAgeIndexFilterProvider2 : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByAge>()
                .Map(person => new PersonByAge
                {
                    Age = person.Age,
                    Adult = person.Age >= 18,
                    Name = person.Firstname
                });
        }
    }
}
