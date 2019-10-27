using System.Text;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class Binary : MapIndex
    {
        public byte[] Content { get; set; }
    }

    public class BinaryIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<Binary>()
                .Map(person => new Binary { Content = Encoding.UTF8.GetBytes(person.Firstname.ToCharArray()) });
        }
    }
}
