using YesSql.Core.Indexes;

namespace YesSql.Samples.FullText.Indexes
{
    public class ArticleByWord : ReduceIndex
    {
        [GroupKey]
        public virtual string Word { get; set; }
        public virtual int Count { get; set; }
    }
}
