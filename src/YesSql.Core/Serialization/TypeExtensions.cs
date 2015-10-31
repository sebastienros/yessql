using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace YesSql.Core.Serialization {

    /// <summary>
    /// Provides a set of extension methods on <see cref="Type"/>
    /// </summary>
    public static class TypeExtensions {
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
            if(IsAnonymousType(typeInfo))
            {
                return "dynamic";
            }

            // todo: make this a service and rename to GetCollection, could also
            // be used for sharding if generic enough
            return _typeNames.GetOrAdd(
                typeInfo, 
                t => String.Concat(type.FullName, ", ", typeInfo.Assembly.GetName().Name));
        }

        private static Dictionary<Type, TypeCode> TypeCodes = new Dictionary<Type, TypeCode>
        {
                {typeof(object), TypeCode.Object},
                {typeof(string), TypeCode.String},
                {typeof(char), TypeCode.Char},
                {typeof(bool), TypeCode.Boolean},
                {typeof(SByte), TypeCode.SByte},
                {typeof(Int16), TypeCode.Int16},
                {typeof(UInt16), TypeCode.UInt16},
                {typeof(Int32), TypeCode.Int32},
                {typeof(UInt32), TypeCode.UInt32},
                {typeof(Int64), TypeCode.Int64},
                {typeof(UInt64), TypeCode.UInt64},
                {typeof(Single), TypeCode.Single},
                {typeof(Double), TypeCode.Double},
                {typeof(Decimal), TypeCode.Decimal},
                {typeof(DateTime), TypeCode.DateTime}
        };

        public static TypeCode GetTypeCode(this Type type)
        {
            TypeCode typeCode;
            if(TypeCodes.TryGetValue(type, out typeCode))
            {
                return typeCode;
            }

            return TypeCode.Empty;
        }
    }
}
