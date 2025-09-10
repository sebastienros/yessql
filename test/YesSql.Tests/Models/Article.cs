using System;
using YesSql.Entites;

namespace YesSql.Tests.Models
{
    public class Article : IVersionable
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime PublishedUtc { get; set; }
        public bool IsPublished { get; set; }
        public long Version { get; set; } = 0;
    }
}