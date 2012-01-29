using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Samples.Hi.Models;

namespace YesSql.Samples.Hi.Indexes
{
    public class BlogPostByAuthor : HasDocumentIndex
    {
        public virtual string Author { get; set; }

        public override void Describe(DescribeContext context)
        {
            // for each BlogPost, create a BlogPostByAuthor index
            context.For<BlogPost, BlogPostByAuthor>().Index(
                map: blogPosts => blogPosts.Select(
                    p => new BlogPostByAuthor {Author = p.Author})
                );
        }
    }
}
