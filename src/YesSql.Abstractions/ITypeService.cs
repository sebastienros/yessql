using System;

namespace YesSql
{
    /// <summary>
    /// An implementation of this interface can provide a way to convert a string to a type.
    /// </summary>
    public interface ITypeService
    {
        /// <summary>
        /// Gets or sets the string representing a type.
        /// </summary>
        string this[Type t] { get; set; }

        /// <summary>
        /// Gets the type represented by a string.
        /// </summary>
        Type this[string s] { get; }
    }
}
