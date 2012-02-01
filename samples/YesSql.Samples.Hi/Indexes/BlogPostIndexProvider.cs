using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Samples.Hi.Models;

namespace YesSql.Samples.Hi.Indexes
{
    public class BlogPostIndexProvider : IndexProvider<BlogPost>
    {
        public override void Describe(DescribeContext<BlogPost> context)
        {
            // for each BlogPost, create a BlogPostByAuthor index
            context.For<BlogPostByAuthor>().Index(
                map: blogPosts => blogPosts.Select(
                    p => new BlogPostByAuthor { Author = p.Author })
                );

            // for each BlogPost, aggregate in an exiting BlogPostByDay
            context.For<BlogPostByDay, string>().Index(
                map:
                    blogPosts => blogPosts.Select(
                    p => new BlogPostByDay {
                            Day = p.PublishedUtc.ToString("yyyyMMdd"),
                            Count = 1
                    }),
                reduce: 
                    group => new BlogPostByDay {
                        Day = group.Key,
                        Count = group.Sum(p => p.Count)
                    },
                delete: 
                    (index, map) => {
                        index.Count -= map.Sum(x => x.Count);

                        // if Count == 0 then delete the index
                        return index.Count > 0 ? index : null;
                    }
            );
        }
    }
}