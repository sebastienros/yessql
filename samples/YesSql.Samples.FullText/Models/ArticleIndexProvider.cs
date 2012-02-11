using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Samples.FullText.Indexes;
using YesSql.Samples.FullText.Tokenizers;

namespace YesSql.Samples.FullText.Models
{
    public class ArticleIndexProvider : IndexProvider<Article>
    {
        public override void Describe(DescribeContext<Article> context)
        {
            var tokenizer = new WhiteSpaceTokenizer();
            var filter = new StopWordFilter();

            // for each BlogPost, create a BlogPostByAuthor index
            context.For<ArticleByWord>().Map(
                article => filter
                    .Filter(tokenizer.Tokenize(article.Content))
                    .Select(x => new ArticleByWord {Word = x})
                );
        }
    }
}