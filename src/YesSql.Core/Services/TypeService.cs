using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YesSql.Indexes;
using YesSql.Serialization;

namespace YesSql.Services
{
    public class TypeService : ITypeService
    {
        private readonly ConcurrentDictionary<Type, string> typeNames = new();

        private readonly ConcurrentDictionary<string, Type> nameTypes = new();

        private static readonly ConcurrentDictionary<PropertyInfo, PropertyInfoAccessor> PropertyAccessors = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> TypeProperties = new();

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



        public PropertyInfo[] GetProperties(Type type)
        {
            if (TypeProperties.TryGetValue(type, out var pis))
            {
                return pis;
            }

            var properties = type.GetProperties().Where(IsWriteable).ToArray();

            var oldType = TypeProperties.FirstOrDefault(x => x.Key.FullName == type.FullName && x.Key != type);
            if (oldType.Key != null)
            {
                TypeProperties.Remove(oldType.Key, out _);
            }

            TypeProperties[type] = properties;
            return properties;
        }

        public PropertyInfoAccessor GetPropertyAccessors(PropertyInfo property, Func<PropertyInfo, PropertyInfoAccessor> createFactory)
        {
            return PropertyAccessors.GetOrAdd(property, createFactory(property));
        }

        private static bool IsWriteable(PropertyInfo pi)
        {
            return
                pi.Name != nameof(IIndex.Id) &&
                // don't read DocumentId when on a MapIndex as it might be used to 
                // read the DocumentId directly from an Index query
                pi.Name != "DocumentId"
                ;
        }
    }
}
