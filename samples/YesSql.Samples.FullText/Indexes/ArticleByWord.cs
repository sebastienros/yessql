using YesSql.Core.Indexes;

namespace YesSql.Samples.FullText.Indexes
{
    public class ArticleByWord : MapIndex
    {
        public virtual string Word { get; set; }
    }
}
