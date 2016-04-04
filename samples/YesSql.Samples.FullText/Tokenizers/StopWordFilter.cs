using System.Collections.Generic;
using System.Linq;

namespace YesSql.Samples.FullText.Tokenizers
{
    public class StopWordFilter : ITokenFilter
    {
        public IEnumerable<string> Filter(IEnumerable<string> tokens)
        {
            return tokens.Where(token => token.Length >= 2);
        }
    }
}
