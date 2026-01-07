using System;
using System.Collections.Concurrent;
using System.Reflection;
using YesSql.Commands;
using YesSql.Serialization;

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


        ConcurrentDictionary<CompoundKey, string> InsertsList { get; set; }
        ConcurrentDictionary<CompoundKey, string> UpdatesList { get; set; }

        void ResetQueryCache()
        {
            InsertsList.Clear();
            UpdatesList.Clear();
        }

        PropertyInfo[] GetProperties(Type type);
        PropertyInfoAccessor GetPropertyAccessors(PropertyInfo property, Func<PropertyInfo, PropertyInfoAccessor> createFactory);
    }
}
