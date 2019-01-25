using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace YesSql.Serialization
{

    /// <summary>
    /// Provides a set of extension methods on <see cref="Type"/>
    /// </summary>
    public static class TypeExtensions
    {
        private static ConcurrentDictionary<TypeInfo, string> _typeNames = new ConcurrentDictionary<TypeInfo, string>();

        /// <summary>
        /// Whether a <see cref="Type"/> is anonymous or not
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <returns><value>true</value> is the type is anonymous, <value>false</value> otherwise.</returns>
        public static bool IsAnonymousType(this TypeInfo type)
        {
            return type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        /// <summary>
        /// Returns a version agnostic type name
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <returns>The name of the type without version information.</returns>
        public static string SimplifiedTypeName(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (IsAnonymousType(typeInfo))
            {
                return "dynamic";
            }

            var customName = typeInfo.GetCustomAttribute<SimplifiedTypeName>();

            // todo: make this a service and rename to GetCollection, could also
            // be used for sharding if generic enough
            return _typeNames.GetOrAdd(
                typeInfo,
                t => String.IsNullOrEmpty(customName?.Name) ? String.Concat(type.FullName, ", ", typeInfo.Assembly.GetName().Name) : customName.Name);
        }
    }
}
