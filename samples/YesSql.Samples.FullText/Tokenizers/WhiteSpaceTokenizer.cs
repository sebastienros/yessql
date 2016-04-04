using System;
using System.Collections.Generic;

namespace YesSql.Samples.FullText.Tokenizers
{
    public class WhiteSpaceTokenizer : ITokenizer
    {
        public IEnumerable<string> Tokenize(string text)
        {
            var start = 0;
            for (var cur = 0; cur < text.Length; cur++)
            {
                if (Char.IsLetter(text[cur])) continue;

                if (cur - start > 1)
                {
                    yield return text.Substring(start, cur - start);
                }

                start = cur + 1;
            }

            if (start != text.Length)
            {
                yield return text.Substring(start);
            }
        }
    }
}
