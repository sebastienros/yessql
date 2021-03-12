using System.Linq;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PersonsByNameCol : ReduceIndex
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class PersonsByNameIndexProviderCol : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonsByNameCol>()
                    .Map(person => new PersonsByNameCol
                    {
                        Name = person.Firstname,
                        Count = 1
                    })
                    .Group(person => person.Name)
                    .Reduce(group => new PersonsByNameCol
                    {
                        Name = group.Key,
                        Count = group.Sum(y => y.Count)
                    })
                    .Delete((index, map) =>
                    {
                        index.Count -= map.Sum(x => x.Count);

                        // if Count == 0 then delete the index
                        return index.Count > 0 ? index : null;
                    });
        }
    }
}
