using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PersonByNameCol : MapIndex
    {
        public string Name { get; set; }
    }

    public class PersonIndexProviderCol : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByNameCol>()
                .Map(person => new PersonByNameCol { Name = person.Firstname });
        }
    }
}
