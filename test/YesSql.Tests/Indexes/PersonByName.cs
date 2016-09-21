using YesSql.Core.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PersonByName : MapIndex
    {
        public string Name { get; set; }
    }

    public class PersonIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByName>()
                .Map(person => new PersonByName { Name = person.Firstname });
        }
    }
}
