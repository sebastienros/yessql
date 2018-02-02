using System;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class ArticleByPublishedDate : MapIndex
    {
        public string Title { get; set; }
        public DateTime PublishedDateTime { get; set; }
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
                    Title = article.Title
                });
        }
    }
}
