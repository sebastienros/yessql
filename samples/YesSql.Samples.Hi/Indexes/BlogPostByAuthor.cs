using YesSql.Core.Indexes;

namespace YesSql.Samples.Hi.Indexes
{
    public class BlogPostByAuthor : MapIndex
    {
        public virtual string Author { get; set; }
    }
}
