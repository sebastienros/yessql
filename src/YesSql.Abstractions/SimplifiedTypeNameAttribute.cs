using System;

namespace YesSql
{
    /// <summary>
    /// Use this attribute to provide a custom string representation of a type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate)]
    public class SimplifiedTypeNameAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the custom name to use for the type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimplifiedTypeNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The custom name to use for the type.</param>
        public SimplifiedTypeNameAttribute(string name)
        {
            Name = name;
        }
    }
}
