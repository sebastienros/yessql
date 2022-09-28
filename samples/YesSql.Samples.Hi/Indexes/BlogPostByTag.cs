using YesSql.Indexes;

namespace YesSql.Samples.Hi.Indexes
{
    public class BlogPostByTag : MapIndex
    {
        public string Tag { get; set; }
    }
}
