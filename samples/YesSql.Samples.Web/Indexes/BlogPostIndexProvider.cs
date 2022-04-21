using System;
using System.Linq;
using YesSql.Indexes;
using YesSql.Samples.Web.Models;

namespace YesSql.Samples.Web.Indexes
{
    public class BlogPostIndex : MapIndex
    {
        public string Title { get; set; }

        public string Author { get; set; }

        public string Content { get; set; }

        public DateTime PublishedUtc { get; set; }

        public bool Published { get; set; }
    }

    public class BlogPostByTag : MapIndex
    {
        public string Tag { get; set; }
    }

    public class BlogPostIndexProvider : IndexProvider<BlogPost>
    {
        public override void Describe(DescribeContext<BlogPost> context)
        {
            context
                .For<BlogPostIndex>()
                .Map(blogPost => new BlogPostIndex
                {
                    Title = blogPost.Title,
                    Author = blogPost.Author,
                    Content = blogPost.Content,
                    PublishedUtc = blogPost.PublishedUtc,
                    Published = blogPost.Published
                });


            // for each BlogPost, create BlogPostByTag index
            context
                .For<BlogPostByTag>()
                .Map(blogPost => blogPost.Tags.Select(tag => new BlogPostByTag { Tag = tag }));
        }
    }
}
