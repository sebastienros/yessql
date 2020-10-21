using System;

namespace YesSql
{
    /// <summary>
    /// Use this attribute to provide a custom string representation of a type.
    /// </summary>
    public class SimplifiedTypeName : Attribute
    {
        public string Name { get; set; }

        public SimplifiedTypeName(string name)
        {
            Name = name;
        }
    }
}
