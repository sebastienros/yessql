using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.Indexes
{
    public class ArticlesByDay : HasDocumentsIndex
    {
        [GroupKey]
        public virtual int DayOfYear { get; set; }
        public virtual int Count { get; set; }

        public override void Describe(DescribeContext context) {
            context
                .For<Article, ArticlesByDay, int>()
                .Index(
                    map: articles => articles.Select(x =>
                        new ArticlesByDay {
                            DayOfYear = x.PublishedUtc.DayOfYear,
                            Count = 1
                        }),
                    reduce: group =>
                        new ArticlesByDay {
                            DayOfYear = group.Key,
                            Count = group.Sum(y => y.Count)
                        },
                    delete: (index, map) => {
                        index.Count -= map.Sum(x => x.Count);

                        // if Count == 0 then delete the index
                        return index.Count > 0 ? index : null;
                    },
                    update: (index, map) => index
                );
        }
    }
}
