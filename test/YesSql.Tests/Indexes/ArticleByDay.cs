using System.Linq;
using YesSql.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class ArticlesByDay : ReduceIndex
    {
        public int Count { get; set; }
        public int DayOfYear { get; set; }
    }

    public class ArticleIndexProvider : IndexProvider<Article>
    {
        public override void Describe(DescribeContext<Article> context)
        {
            context
                .For<ArticlesByDay, int>()
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
}
