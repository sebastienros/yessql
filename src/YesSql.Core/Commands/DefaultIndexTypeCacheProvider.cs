using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using YesSql.Indexes;
using YesSql.Serialization;

namespace YesSql.Commands
{
    public class DefaultIndexTypeCacheProvider : IIndexTypeCacheProvider
    {
        private static readonly ConcurrentDictionary<PropertyInfo, PropertyInfoAccessor> PropertyAccessors = new();
        private static readonly ConcurrentDictionary<string, PropertyInfo[]> TypeProperties = new();

        public PropertyInfoAccessor GetPropertyAccessor(PropertyInfo property)
        {
            return PropertyAccessors.GetOrAdd(property, p => new PropertyInfoAccessor(p));
        }

        public PropertyInfo[] GetTypeProperties(Type type)
        {
            if (TypeProperties.TryGetValue(type.FullName, out var pis))
            {
                return pis;
            }

            var properties = type.GetProperties().Where(IsWriteable).ToArray();
            TypeProperties[type.FullName] = properties;
            return properties;
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
