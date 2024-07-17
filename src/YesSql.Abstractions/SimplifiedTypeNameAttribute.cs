using System;

namespace YesSql
{
    /// <summary>
    /// Use this attribute to provide a custom string representation of a type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate)]
    public class SimplifiedTypeNameAttribute : Attribute
    {
        public string Name { get; set; }

        public SimplifiedTypeNameAttribute(string name) => Name = name;
    }
}
