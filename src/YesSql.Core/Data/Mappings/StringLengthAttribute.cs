using System;

namespace YesSql.Core.Data.Mappings {
    public class StringLengthAttribute : Attribute 
    {
        public StringLengthAttribute(int length)
        {
            Length = length;
        }

        public int Length { get; private set; }
    }
}
