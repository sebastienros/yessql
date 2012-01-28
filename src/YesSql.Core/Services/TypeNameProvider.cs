using System;

namespace YesSql.Core.Services {

    /// <summary>
    /// Provides a method returning a version agnostic type name
    /// </summary>
    internal static class TypeNameProvider
    {
        public static string SimplifiedTypeName(this Type type)
        {
            return String.Concat(type.FullName, ", ", type.Assembly.GetName().Name);
        }
    }
}
