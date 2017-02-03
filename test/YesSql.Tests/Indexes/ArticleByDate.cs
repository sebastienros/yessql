using System;
using YesSql.Core.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class ArticleByPublishedDate : MapIndex
    {
        public DateTime PublishedDateTime { get; set; }
        //public DateTimeOffset PublishedDateTimeOffset { get; set; }
    }

    public class ArticleBydPublishedDateProvider : IndexProvider<Article>
    {
        public override void Describe(DescribeContext<Article> context)
        {
            context
                .For<ArticleByPublishedDate>()
                .Map(article => new ArticleByPublishedDate
                {
                    PublishedDateTime = article.PublishedUtc,
                    //PublishedDateTimeOffset = article.PublishedUtc
                });
        }
    }
}
