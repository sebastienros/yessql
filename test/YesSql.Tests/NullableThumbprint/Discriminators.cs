using System;

namespace YesSql.Tests.NullableThumbprint
{
    public class DiscriminatorWithStrings
    {
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
    }

    public class DiscriminatorWithNullable
    {
        public int? A { get; set; }
        public bool? B { get; set; }
        public DateTime? C { get; set; }
    }

    public class DiscriminatorWithNoNullable
    {
        public int A { get; set; }
        public bool B { get; set; }
        public DateTime C { get; set; }
    }

    public class DiscriminatorWithNoNullable2 : DiscriminatorWithNoNullable
    {
    }
}
