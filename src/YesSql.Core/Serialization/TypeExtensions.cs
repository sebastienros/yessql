using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace YesSql.Core.Serialization {

    /// <summary>
    /// Provides a set of extension methods on <see cref="Type"/>
    /// </summary>
    public static class TypeExtensions {
        private static ConcurrentDictionary<Type, string> _typeNames = new ConcurrentDictionary<Type, string>();

        /// <summary>
        /// Whether a <see cref="Type"/> is anonymous or not
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <returns><value>true</value> is the type is anonymous, <value>false</value> otherwise.</returns>
        public static bool IsAnonymousType(this Type type)
        {
            return Attribute.IsDefined(type, typeof (CompilerGeneratedAttribute), false)
                   && type.IsGenericType && type.Name.Contains("AnonymousType")
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
            if(IsAnonymousType(type))
            {
                return "dynamic";
            }

            // todo: make this a service and rename to GetCollection, could also
            // be used for sharding if generic enough
            return _typeNames.GetOrAdd(type, t => String.Concat(type.FullName, ", ", type.Assembly.GetName().Name));
        }
    }
}
