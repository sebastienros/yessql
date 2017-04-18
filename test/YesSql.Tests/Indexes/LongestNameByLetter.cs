//using System.Linq;
//using YesSql.Indexes;
//using YesSql.Tests.Models;

//namespace YesSql.Tests.Indexes
//{
//    public class LongestNameByLetter : ReduceIndex
//    {
//        public char Letter { get; set; }
//        public int Length { get; set; }
//        public string Name { get; set; }
//    }

//    public class LongestNameByLetterIndexProvider : IndexProvider<Person>
//    {
//        public override void Describe(DescribeContext<Person> context)
//        {
//            context
//                .For<LongestNameByLetter, int>()
//                    .Map(person => new LongestNameByLetter
//                    {
//                        Letter = person.Firstname.FirstOrDefault(),
//                        Name = person.Firstname,
//                        Length = person.Firstname.Length
//                    })
//                    .Group(index => index.Letter)
//                    .Reduce(group => {
//                        var longest = group.OrderBy(x => x.Name.Length).FirstOrDefault();
//                        return new LongestNameByLetter
//                        {
//                            Letter = longest.Letter,
//                            Length = longest.Length,
//                            Name = longest.Name
//                        };
//                    })
//                    ;
//        }
//    }
//}
