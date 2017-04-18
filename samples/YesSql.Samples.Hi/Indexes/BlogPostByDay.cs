using YesSql.Indexes;

namespace YesSql.Samples.Hi.Indexes
{
    public class BlogPostByDay : ReduceIndex
    {
        public string Day { get; set; }
        public int Count { get; set; }
    }
}
