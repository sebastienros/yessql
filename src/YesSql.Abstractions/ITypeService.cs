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
        ConcurrentDictionary<CompoundKey, string> ExecuteInsertsList { get; set; }
        ConcurrentDictionary<CompoundKey, string> ExecuteUpdatesList { get; set; }

        /// <summary>
        /// Clears the statement caches. The lists are optional in minimal implementations,
        /// so the default implementation is null-conditional (a plain <c>.Clear()</c> here
        /// would throw <see cref="NullReferenceException"/> for such implementations).
        /// </summary>
        void ResetQueryCache()
        {
            InsertsList?.Clear();
            UpdatesList?.Clear();
            ExecuteInsertsList?.Clear();
            ExecuteUpdatesList?.Clear();
        }

        /// <summary>
        /// Forgets all cached state for a single type. Default implementation does nothing;
        /// <c>TypeService</c> clears its per-type entries.
        /// </summary>
        void ForgetType(Type type)
        {
        }

        PropertyInfo[] GetProperties(Type type);
        PropertyInfoAccessor GetPropertyAccessors(PropertyInfo property, Func<PropertyInfo, PropertyInfoAccessor> createFactory);
    }
}
