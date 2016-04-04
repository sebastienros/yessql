using System;

namespace YesSql.Tests.Models
{
    public class Article
    {
        public int Id { get; set; }
        public DateTime PublishedUtc { get; set; }
        public bool IsPublished { get; set; }
    }
}