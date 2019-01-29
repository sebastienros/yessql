using System;
using System.Reflection;

namespace YesSql.Data
{
    /// <summary>
    /// Provides an <see cref="IIdentifierFactory"/> implementation that creates <see cref="IIdAccessor{T}"/>
    /// instances for a specific property name of an object.
    /// </summary>
    public class DefaultIdentifierFactory : IIdentifierFactory
    {
        public IIdAccessor<T> CreateAccessor<T>(Type tContainer, string name)
        {
            var propertyName = name;
            var propertyInfo = tContainer.GetProperty(propertyName);

            if (propertyInfo == null)
            {
                return null;
            }

            var tProperty = propertyInfo.PropertyType;

            var getType = typeof(Func<,>).MakeGenericType(new[] { tContainer, tProperty });
            var setType = typeof(Action<,>).MakeGenericType(new[] { tContainer, tProperty });

            var getter = propertyInfo.GetGetMethod().CreateDelegate(getType);
            var setter = propertyInfo.GetSetMethod(true).CreateDelegate(setType);

            var accessorType = typeof(IdAccessor<,>).MakeGenericType(tContainer, tProperty);

            return Activator.CreateInstance(accessorType, new object[] { getter, setter }) as IIdAccessor<T>;
        }

    }
}