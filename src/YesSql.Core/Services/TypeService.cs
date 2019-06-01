using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace YesSql.Services
{
    public class TypeService : ITypeService
    {
        private readonly ConcurrentDictionary<Type, string> typeNames = new ConcurrentDictionary<Type, string>();

        private readonly ConcurrentDictionary<string, Type> nameTypes = new ConcurrentDictionary<string, Type>();

        public IEnumerable<Type> Keys { get { return typeNames.Keys; } }

        public IEnumerable<string> Values { get { return typeNames.Values; } }

        public Type ReverseLookup(string value)
        {
            if (value == "dynamic")
            {
                return null;
            }

            return nameTypes[value];
        }

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

                    var customName = typeInfo.GetCustomAttribute<SimplifiedTypeName>();
                    var actualName = String.IsNullOrEmpty(customName?.Name) ? String.Concat(type.FullName, ", ", typeInfo.Assembly.GetName().Name) : customName.Name;
                    nameTypes[actualName] = t;

                    return actualName;
                });
            }

            set
            {
                typeNames[t] = value;
                nameTypes[value] = t;
            }
        }

        private bool IsAnonymousType(TypeInfo type)
        {
            return type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }
    }
}
