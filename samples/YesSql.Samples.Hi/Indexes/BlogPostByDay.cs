using YesSql.Core.Indexes;

namespace YesSql.Samples.Hi.Indexes
{
    public class BlogPostByDay : ReduceIndex
    {
        public virtual string Day { get; set; }
        public virtual int Count { get; set; }
    }
}
