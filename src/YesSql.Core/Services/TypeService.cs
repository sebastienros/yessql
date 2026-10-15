using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YesSql.Commands;
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

        public ConcurrentDictionary<CompoundKey, string> InsertsList { get; set; } = new();
        public ConcurrentDictionary<CompoundKey, string> UpdatesList { get; set; } = new();
        public ConcurrentDictionary<CompoundKey, string> ExecuteInsertsList { get; set; } = new();
        public ConcurrentDictionary<CompoundKey, string> ExecuteUpdatesList { get; set; } = new();

        public void ResetQueryCache()
        {
            UpdatesList.Clear();
            InsertsList.Clear();
            ExecuteInsertsList.Clear();
            ExecuteUpdatesList.Clear();
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
            // Fast path: skip accessor construction (delegate creation) when one is already cached.
            if (PropertyAccessors.TryGetValue(property, out var cached))
            {
                return cached;
            }

            return PropertyAccessors.GetOrAdd(property, createFactory);
        }

        /// <summary>
        /// Forgets all cached state for a single type: its property list, its property accessors,
        /// its name-map entries and the per-type entries in the four statement caches.
        /// </summary>
        public void ForgetType(Type type)
        {
            if (typeNames.TryGetValue(type, out var typeName))
            {
                typeNames.TryRemove(type, out _);
                nameTypes.TryRemove(typeName, out _);
            }

            TypeProperties.TryRemove(type, out _);

            foreach (var key in PropertyAccessors.Keys)
            {
                if (key.DeclaringType == type)
                {
                    PropertyAccessors.TryRemove(key, out _);
                }
            }

            var fullName = type.FullName;
            foreach (var statements in new[] { InsertsList, UpdatesList, ExecuteInsertsList, ExecuteUpdatesList })
            {
                foreach (var key in statements.Keys)
                {
                    if (key.Type == fullName)
                    {
                        statements.TryRemove(key, out _);
                    }
                }
            }
        }

        private static bool IsWriteable(PropertyInfo pi)
        {
            return
                pi.Name != nameof(IIndex.Id) &&
                // don't read DocumentId when on a MapIndex as it might be used to
                // read the DocumentId directly from an Index query
                pi.Name != "DocumentId";
        }
    }
}
