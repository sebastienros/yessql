using System.Linq;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class SingleRecordPersonIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByName>()
                .SingleRecordPerDocument()
                .Map(person => new PersonByName { SomeName = person.Firstname });
        }
    }

    public class SingleRecordPersonAgeIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByAge>()
                .SingleRecordPerDocument()
                .Map(person => new PersonByAge
                {
                    Age = person.Age,
                    Adult = person.Age >= 18,
                    Name = person.Firstname
                });
        }
    }

    public class SingleRecordExclusivePersonIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByName>()
                .ExclusiveToDocumentType()
                .SingleRecordPerDocument()
                .Map(person => new PersonByName { SomeName = person.Firstname });
        }
    }

    public class SecondSingleRecordPersonIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByName>()
                .SingleRecordPerDocument()
                .Map(person => new PersonByName { SomeName = person.Lastname });
        }
    }

    public class MultipleRecordsPersonAgeIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByAge>()
                .Map(person => new[]
                {
                    new PersonByAge { Age = person.Age, Name = person.Firstname },
                    new PersonByAge { Age = person.Age + 1, Name = person.Firstname }
                }.AsEnumerable());
        }
    }

    public class SingleRecordInvalidEmailIndexProvider : IndexProvider<Email>
    {
        public override void Describe(DescribeContext<Email> context)
        {
            context
                .For<EmailByAttachment>()
                .SingleRecordPerDocument()
                .Map(e => e.Attachments.Select(a => new EmailByAttachment()
                {
                    Date = e.Date,
                    AttachmentName = a.Name
                }));
        }
    }

    public class SingleRecordArticleIndexProvider : IndexProvider<Article>
    {
        public override void Describe(DescribeContext<Article> context)
        {
            context
                .For<ArticlesByDay, int>()
                .SingleRecordPerDocument()
                .Map(article => new ArticlesByDay
                {
                    DayOfYear = article.PublishedUtc.DayOfYear,
                    Count = 1
                })
                .Group(article => article.DayOfYear)
                .Reduce(group => new ArticlesByDay
                {
                    DayOfYear = group.Key,
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

    public class SingleRecordShapeIndexProvider<TShape> : IndexProvider<TShape> where TShape : Shape
    {
        public override void Describe(DescribeContext<TShape> context)
        {
            context
                .For<ShapeIndex>()
                .SingleRecordPerDocument()
                .Map(shape => new ShapeIndex { Name = typeof(TShape).Name });
        }
    }
}
