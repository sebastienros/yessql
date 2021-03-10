using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class AnotherPersonByAge : PersonByAge
    {
        public string AdditionalField1 { get; set; }
        public string AdditionalField2 { get; set; }
        public string AdditionalField3 { get; set; }
    }

    public class AnotherPersonAgeIndexProvider : IndexProvider<AnotherPerson>
    {
        public override void Describe(DescribeContext<AnotherPerson> context)
        {
            context
                .For<AnotherPersonByAge>()
                .Map(person => new AnotherPersonByAge
                {
                    Age = person.Age,
                    Adult = person.Age >= 18,
                    Name = person.Firstname,
                    AdditionalField1 = person.AdditionalField1,
                    AdditionalField2 = person.AdditionalField2,
                    AdditionalField3 = person.AdditionalField3
                });
        }
    }
}
