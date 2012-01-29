using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PersonByName : HasDocumentIndex
    {
        public virtual string Name { get; set; }

        public override void Describe(DescribeContext context) {
            context
                .For<Person, PersonByName>()
                .Index(
                    map: persons => persons.Select( p => new PersonByName { Name = p.Firstname })
            );
        }
    }
}
