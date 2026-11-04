using System.Linq;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class ExclusivePersonIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByName>()
                .ExclusiveToDocumentType()
                .Map(person => new PersonByName { SomeName = person.Firstname });
        }
    }

    public class ExclusivePersonAgeIndexProvider : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByAge>()
                .ExclusiveToDocumentType()
                .Map(person => new PersonByAge
                {
                    Age = person.Age,
                    Adult = person.Age >= 18,
                    Name = person.Firstname
                });
        }
    }

    public class ExclusivePersonIndexProviderCol : IndexProvider<Person>
    {
        public override void Describe(DescribeContext<Person> context)
        {
            context
                .For<PersonByNameCol>()
                .ExclusiveToDocumentType()
                .Map(person => new PersonByNameCol { Name = person.Firstname });
        }
    }

    public class ExclusiveEmailByAttachmentProvider : IndexProvider<Email>
    {
        public override void Describe(DescribeContext<Email> context)
        {
            context
                .For<EmailByAttachment>()
                .ExclusiveToDocumentType()
                .Map(e => e.Attachments.Select(a => new EmailByAttachment()
                {
                    Date = e.Date,
                    AttachmentName = a.Name
                }));
        }
    }

    public class ExclusiveArticleIndexProvider : IndexProvider<Article>
    {
        public override void Describe(DescribeContext<Article> context)
        {
            context
                .For<ArticlesByDay, int>()
                .ExclusiveToDocumentType()
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

    public class ExclusiveArticleByPublishedDateProvider : IndexProvider<Article>
    {
        public override void Describe(DescribeContext<Article> context)
        {
            context
                .For<ArticleByPublishedDate>()
                .ExclusiveToDocumentType()
                .Map(article => new ArticleByPublishedDate
                {
                    PublishedDateTime = article.PublishedUtc,
                    Title = article.Title
                });
        }
    }
}
