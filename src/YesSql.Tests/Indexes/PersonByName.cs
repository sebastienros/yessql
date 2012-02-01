using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PersonByName : MapIndex
    {
        public virtual string Name { get; set; }
    }

    public class PersonIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context) 
        {
            context
                .For<PersonByName>()
                .Index(
                    map: persons => persons.Select(p => new PersonByName { Name = p.Firstname })
            );
        }
    }
}
