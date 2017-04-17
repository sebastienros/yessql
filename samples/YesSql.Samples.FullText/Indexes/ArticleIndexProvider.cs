using System.Linq;
using YesSql.Indexes;
using YesSql.Samples.FullText.Models;
using YesSql.Samples.FullText.Tokenizers;

namespace YesSql.Samples.FullText.Indexes
{
    public class ArticleIndexProvider : IndexProvider<Article>
    {
        public override void Describe(DescribeContext<Article> context)
        {
            var tokenizer = new WhiteSpaceTokenizer();
            var filter = new StopWordFilter();

            // for each BlogPost, create a BlogPostByAuthor index
            context.For<ArticleByWord, string>()
                .Map(article => filter
                    .Filter(tokenizer.Tokenize(article.Content))
                    .Select(x => new ArticleByWord { Word = x, Count = 1 })
                )
                .Group(article => article.Word)
                .Reduce(group => new ArticleByWord
                {
                    Word = group.Key,
                    Count = group.Sum(y => y.Count)
                })
                .Delete((index, map) =>
                {
                    index.Count -= map.Sum(x => x.Count);

                    // if Count == 0 then delete the index
                    return index.Count > 0 ? index : null;
                });
        }
    }
}