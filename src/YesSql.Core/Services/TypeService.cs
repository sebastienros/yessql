using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace YesSql.Services
{
    public class TypeService : ITypeService
    {
        private readonly ConcurrentDictionary<Type, string> typeNames = new();

        private readonly ConcurrentDictionary<string, Type> nameTypes = new();

        public string this[Type t]
        {
            get
            {
                return typeNames.GetOrAdd(t, type =>
                {
                    var typeInfo = t.GetTypeInfo();
                    if (IsAnonymousType(typeInfo))
                    {
                        return "dynamic";
                    }

                    var customName = typeInfo.GetCustomAttribute<SimplifiedTypeNameAttribute>();
                    var calculatedName = string.IsNullOrEmpty(customName?.Name) ? $"{type.FullName}, {typeInfo.Assembly.GetName().Name}" : customName.Name;
                    nameTypes[calculatedName] = t;

                    return calculatedName;
                });
            }

            set
            {
                typeNames[t] = value;
                nameTypes[value] = t;
            }
        }

        public Type this[string s]
        {
            get
            {
                if (s == "dynamic")
                {
                    return typeof(object);
                }

                // The reverse map is populated lazily, only when a type is first written or
                // queried by its exact type during the current process. When reading a row whose
                // type hasn't been registered yet, fall back to resolving it through reflection
                // instead of throwing a 'KeyNotFoundException'. The resolved type is cached to
                // avoid repeated reflection lookups. If the type name can't be resolved at all, a
                // 'TypeResolutionException' is thrown so the document is not silently ignored.
                return nameTypes.GetOrAdd(s, static name =>
                    Type.GetType(name, throwOnError: false) ?? throw new TypeResolutionException(name));
            }
        }

        private static bool IsAnonymousType(TypeInfo type)
        {
            return type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && (type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic;
        }
    }
}
