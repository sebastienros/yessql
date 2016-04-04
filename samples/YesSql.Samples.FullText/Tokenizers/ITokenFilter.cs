using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YesSql.Samples.FullText.Tokenizers
{
    public interface ITokenFilter
    {
        IEnumerable<string> Filter(IEnumerable<string> tokens);
    }
}
