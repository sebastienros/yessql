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

            Type accessorType = null;
            
            if (tProperty == typeof(int))
            {
                accessorType = typeof(IntAccessor<>);
            }
            else if (tProperty == typeof(long))
            {
                accessorType = typeof(LongAccessor<>);
            }

            if (accessorType == null)
            {
                // Id type is not supported
                return null;
            }

            accessorType = accessorType.MakeGenericType(tContainer);

            return Activator.CreateInstance(accessorType, new object[] { getter, setter }) as IAccessor<T>;
        }

        /// <summary>
        /// An accessor to an Int32 Id property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal class IntAccessor<T> : IAccessor<long>
        {
            private readonly Func<T, int> _getter;
            private readonly Action<T, int> _setter;

            public IntAccessor(Func<T, int> getter, Action<T, int> setter)
            {
                _getter = getter;
                _setter = setter;
            }

            long IAccessor<long>.Get(object obj)
            {
                return _getter((T)obj);
            }

            void IAccessor<long>.Set(object obj, long value)
            {
                _setter((T)obj, (int)value);
            }
        }

        /// <summary>
        /// An accessor to an Int64 Id property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal class LongAccessor<T> : IAccessor<long>
        {
            private readonly Func<T, long> _getter;
            private readonly Action<T, long> _setter;

            public LongAccessor(Func<T, long> getter, Action<T, long> setter)
            {
                _getter = getter;
                _setter = setter;
            }

            long IAccessor<long>.Get(object obj)
            {
                return _getter((T)obj);
            }

            void IAccessor<long>.Set(object obj, long value)
            {
                _setter((T)obj, value);
            }
        }
    }
}
