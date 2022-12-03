using System.Collections.Generic;

namespace YesSql.Samples.FullText.Tokenizers
{
    public interface ITokenFilter
    {
        IEnumerable<string> Filter(IEnumerable<string> tokens);
    }
}
