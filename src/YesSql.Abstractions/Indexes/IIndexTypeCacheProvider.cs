using System;
using System.Reflection;
using YesSql.Serialization;

namespace YesSql.Indexes
{
    public interface IIndexTypeCacheProvider
    {
        public PropertyInfo[] GetTypeProperties(Type type);
        public PropertyInfoAccessor GetPropertyAccessor(PropertyInfo property);
    }
}
