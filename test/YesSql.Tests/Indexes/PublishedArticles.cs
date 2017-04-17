using System.Linq;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class PublishedArticle : MapIndex
    {
    }

    public class PublishedArticleIndexProvider : IndexProvider<Article>
    {
        public override void Describe(DescribeContext<Article> context)
        {
            context
                .For<PublishedArticle>()
                .Map(article =>
                    article.IsPublished ? new PublishedArticle() : null
                );
        }
    }
}
