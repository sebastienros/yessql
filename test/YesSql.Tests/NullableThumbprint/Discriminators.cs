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

    public class DiscriminatorWithMaxNullable
    {
        public string A01 { get; set; }
        public string A02 { get; set; }
        public string A03 { get; set; }
        public string A04 { get; set; }
        public string A05 { get; set; }
        public string A06 { get; set; }
        public string A07 { get; set; }
        public string A08 { get; set; }
        public string A09 { get; set; }
        public string A10 { get; set; }
        public string A11 { get; set; }
        public string A12 { get; set; }
        public string A13 { get; set; }
        public string A14 { get; set; }
        public string A15 { get; set; }
        public string A16 { get; set; }
        public string A17 { get; set; }
        public string A18 { get; set; }
        public string A19 { get; set; }
        public string A20 { get; set; }
        public string A21 { get; set; }
        public string A22 { get; set; }
        public string A23 { get; set; }
        public string A24 { get; set; }
        public string A25 { get; set; }
        public string A26 { get; set; }
        public string A27 { get; set; }
        public string A28 { get; set; }
        public string A29 { get; set; }
        public string A30 { get; set; }
        public string A31 { get; set; }
        public string A32 { get; set; }
        public string A33 { get; set; }
        public string A34 { get; set; }
        public string A35 { get; set; }
        public string A36 { get; set; }
        public string A37 { get; set; }
        public string A38 { get; set; }
        public string A39 { get; set; }
        public string A40 { get; set; }
        public string A41 { get; set; }
        public string A42 { get; set; }
        public string A43 { get; set; }
        public string A44 { get; set; }
        public string A45 { get; set; }
        public string A46 { get; set; }
        public string A47 { get; set; }
        public string A48 { get; set; }
    }

    public class DiscriminatorWithMaxNullable2 : DiscriminatorWithMaxNullable
    {
        public string A49 { get; set; }
    }
}
