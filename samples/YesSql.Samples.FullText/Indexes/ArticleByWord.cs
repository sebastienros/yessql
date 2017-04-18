using YesSql.Indexes;

namespace YesSql.Samples.FullText.Indexes
{
    public class ArticleByWord : ReduceIndex
    {
        public string Word { get; set; }
        public int Count { get; set; }
    }
}
