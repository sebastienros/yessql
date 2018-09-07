using System.Threading.Tasks;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PersonByName : MapIndex
    {
        public string SomeName { get; set; }
        public static string Normalize(string name)
        {
            return name.ToUpperInvariant();
        }
    }

    public class PersonIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByName>()
                .Map(person => new PersonByName { SomeName = person.Firstname });
        }
    }

    public class PersonAsyncIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByName>()
                .Map(async person =>
                {
                    await Task.Delay(10);
                    return new PersonByName { SomeName = person.Firstname };
                });
        }
    }

}
