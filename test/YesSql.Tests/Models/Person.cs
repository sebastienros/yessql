using System.Collections.Generic;

namespace YesSql.Tests.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public int Age { get; set; }
        public bool Anonymous { get; set; }
        public int Version { get; set; }
        public List<string> Nationalities { get; set; }
    }
}
