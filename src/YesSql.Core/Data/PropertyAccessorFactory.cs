using System;
using System.Reflection;

namespace YesSql.Data
{
    /// <summary>
    /// Provides an <see cref="IAccessorFactory"/> implementation that creates <see cref="IAccessor{T}"/>
    /// instances for a specific property name of an object.
    /// </summary>
    public class PropertyAccessorFactory : IAccessorFactory
    {
        const BindingFlags DefaultBindingFlags = BindingFlags.IgnoreCase 
            | BindingFlags.Public  
            | BindingFlags.Instance 
            | BindingFlags.GetProperty 
            | BindingFlags.SetProperty 
            ;

        private readonly string _propertyName;

        public PropertyAccessorFactory(string propertyName)
        {
            _propertyName = propertyName;
        }

        public IAccessor<T> CreateAccessor<T>(Type tContainer)
        {
            var propertyInfo = tContainer.GetProperty(_propertyName, DefaultBindingFlags);

            if (propertyInfo == null)
            {
                return null;
            }

            var tProperty = propertyInfo.PropertyType;

            var getType = typeof(Func<,>).MakeGenericType(new[] { tContainer, tProperty });
            var setType = typeof(Action<,>).MakeGenericType(new[] { tContainer, tProperty });

            var getter = propertyInfo.GetGetMethod().CreateDelegate(getType);
            var setter = propertyInfo.GetSetMethod(true).CreateDelegate(setType);

            var accessorType = typeof(IAccessor<,>).MakeGenericType(tContainer, tProperty);

            return Activator.CreateInstance(accessorType, new object[] { getter, setter }) as IAccessor<T>;
        }

        private class IAccessor<T, TU> : IAccessor<TU>
        {
            private readonly Func<T, TU> _getter;
            private readonly Action<T, TU> _setter;

            public IAccessor(Func<T, TU> getter, Action<T, TU> setter)
            {
                _getter = getter;
                _setter = setter;
            }

            TU IAccessor<TU>.Get(object obj)
            {
                return _getter((T)obj);
            }

            void IAccessor<TU>.Set(object obj, TU value)
            {
                _setter((T)obj, value);
            }
        }
    }
}
