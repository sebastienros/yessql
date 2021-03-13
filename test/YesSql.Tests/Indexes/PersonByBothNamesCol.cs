using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PersonByBothNamesCol : MapIndex
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
    }

    public class PersonIndexBothNamesProviderCol : IndexProvider<Person>
    {        
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByBothNamesCol>()
                .Map(person => new PersonByBothNamesCol { Firstname = person.Firstname, Lastname = person.Lastname });
        }
    }
}
