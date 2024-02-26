using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YesSql.Serialization;

namespace YesSql.Indexes
{
    public class IndexTypeCacheProvider
    {
        private static readonly ConcurrentDictionary<PropertyInfo, PropertyInfoAccessor> PropertyAccessors = new();
        private static readonly ConcurrentDictionary<string, PropertyInfo[]> TypeProperties = new();

        public virtual PropertyInfoAccessor GetPropertyAccessor(PropertyInfo property) => PropertyAccessors.GetOrAdd(property, p => new PropertyInfoAccessor(p));

        public virtual PropertyInfo[] GetTypeProperties(Type type)
        {
            if (TypeProperties.TryGetValue(type.FullName, out var pis))
            {
                return pis;
            }

            var properties = type.GetProperties().Where(IsWriteable).ToArray();
            TypeProperties[type.FullName] = properties;

            return properties;
        }

        public virtual void UpdateCachedType(Type type)
        {
            if (TypeProperties.TryRemove(type.FullName, out var pis))
            {
                foreach (var prop in pis)
                {
                    PropertyAccessors.TryRemove(prop, out _);
                }
            }

            var properties = type.GetProperties().Where(IsWriteable).ToArray();
            TypeProperties[type.FullName] = properties;
        }

        protected bool IsWriteable(PropertyInfo pi)
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
