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
                .Map(blogPost =>
                   new BlogPostByAuthor { Author = blogPost.Author }
                );

            // for each BlogPost, aggregate in an exiting BlogPostByDay
            context.For<BlogPostByDay, string>()
                .Map(blogPost =>
                   new BlogPostByDay
                   {
                       Day = blogPost.PublishedUtc.ToString("yyyyMMdd"),
                       Count = 1
                   })
                .Group(blogPost => blogPost.Day)
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
        }
    }
}