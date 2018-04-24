using YesSql.Indexes;

namespace YesSql.Samples.Gating
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
}
