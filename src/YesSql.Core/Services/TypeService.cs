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

                return nameTypes[s];
            }
        }

        private static bool IsAnonymousType(TypeInfo type)
        {
            return type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }
    }
}
