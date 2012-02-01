using System.Linq;
using YesSql.Core.Indexes;
using YesSql.Samples.Hi.Models;

namespace YesSql.Samples.Hi.Indexes
{
    public class BlogPostByAuthor : MapIndex
    {
        public virtual string Author { get; set; }
    }
}
