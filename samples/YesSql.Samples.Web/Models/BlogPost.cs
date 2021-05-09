using System;

namespace YesSql.Samples.Web.Models
{
    public class BlogPost
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Author { get; set; }

        public string Content { get; set; }

        public DateTime PublishedUtc { get; set; }
        public bool Published { get; set; }

        public string[] Tags { get; set; }
    }
}
