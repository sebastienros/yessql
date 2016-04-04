using System.Collections.Generic;

namespace YesSql.Samples.FullText.Tokenizers
{
    public interface ITokenizer
    {
        IEnumerable<string> Tokenize(string text);
    }
}
