using YesSql.Core.Indexes;

namespace YesSql.Samples.Hi.Indexes
{
    public class BlogPostByDay : ReduceIndex
    {
        [GroupKey]
        public virtual string Day { get; set; }
        public virtual int Count { get; set; }
    }
}
