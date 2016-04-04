using System;

namespace YesSql.Samples.Hi.Models
{
    public class BlogPost
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Content { get; set; }
        public DateTime PublishedUtc { get; set; }
        public string[] Tags { get; set; }
    }
}
