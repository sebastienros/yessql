using System.Collections.Generic;
using System.Linq;
using YesSql.Indexes;
using YesSql.Samples.Hi.Models;

namespace YesSql.Samples.Hi.Indexes
{
    public class BlogPostIndexProvider : IndexProvider<BlogPost>
    {
        public override void Describe(DescribeContext<BlogPost> context)
        {
            // for each BlogPost, create a BlogPostByAuthor index
            context.For<BlogPostByAuthor>()
                .Map(x =>
                   new BlogPostByAuthor { Author = x.Author }
                );

            // for each BlogPost, aggregate in an exiting BlogPostByDay
            context.For<BlogPostByDay, string>()
                .Map(x =>
                   new BlogPostByDay
                   {
                       Day = x.PublishedUtc.ToString("yyyyMMdd"),
                       Count = 1
                   })
                .Group(x => x.Day)
                .Reduce(group =>
                   new BlogPostByDay
                   {
                       Day = group.Key,
                       Count = group.Sum(p => p.Count)
                   })
                .Delete((index, map) =>
                   {
                       index.Count -= map.Sum(x => x.Count);

                       // if Count == 0 then delete the index
                       return index.Count > 0 ? index : null;
                   }
            );

            // for each BlogPost, create a BlogPostByTag index
            context.For<BlogPostByTag>()
                .Map(x => x.Tags.Select(y => new BlogPostByTag { Tag = y }));
        }
    }
}