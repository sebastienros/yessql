using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace YesSql.Services
{
    public class TypeService : ITypeService
    {
        private readonly ConcurrentDictionary<Type, string> typeNames = new ConcurrentDictionary<Type, string>();

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

                    return String.IsNullOrEmpty(customName?.Name) ? String.Concat(type.FullName, ", ", typeInfo.Assembly.GetName().Name) : customName.Name;
                });
            }

            set
            {
                typeNames[t] = value;
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
