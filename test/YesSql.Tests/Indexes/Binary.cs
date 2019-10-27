using System;
using System.Text;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class Binary : MapIndex
    {
        public byte[] Content1 { get; set; }
        public byte[] Content2 { get; set; }
        public byte[] Content3 { get; set; }
        public byte[] Content4 { get; set; }
        public byte[] Content5 { get; set; }
    }

    public class BinaryIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<Binary>()
                .Map(person => new Binary {
                    Content1 = new byte[255],
                    Content2 = new byte[65535],
                    //Content3 = new byte[16777215],
                    //Content4 = new byte[16777216],
                    Content5 = new byte[8000]
                });
        }
    }
}
