using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PersonByLastnameCol : MapIndex
    {
        public string Lastname { get; set; }
    }

    public class PersonIndexLastnameProviderCol : IndexProvider<Person>
    {
        public PersonIndexLastnameProviderCol() => CollectionName = "Collection1";
        
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByLastnameCol>()
                .Map(person => new PersonByLastnameCol { Lastname = person.Lastname });
        }
    }
}
