using System;
using System.Collections.Generic;
using System.Threading;

namespace YesSql.Data
{
    /// <summary>
    /// Compiled queries have their SQL cached, and only the parameters are changed.
    /// If nullable arguments (including strings) are used, the SQL should vary.
    /// This class allows to generate a Thumbprint for each set of nullable compiled query properties.
    /// </summary>
    internal sealed class NullableThumbprintFactory
    {
        private static readonly Dictionary<Type, NullableThumbprintBuilder> _discriminatorFactories = new();

        internal static NullableThumbprintBuilder GetNullableThumbprintBuilder(Type type)
        {
            if (!_discriminatorFactories.TryGetValue(type, out var nullDiscriminatorBuilder))
            {
                lock (_discriminatorFactories)
                {
                    if (!_discriminatorFactories.TryGetValue(type, out nullDiscriminatorBuilder))
                    {
                        nullDiscriminatorBuilder = new NullableThumbprintBuilder(type);
                        _discriminatorFactories[type] = nullDiscriminatorBuilder;
                    }
                }
            }

            return nullDiscriminatorBuilder;
        }

        public static long GetNullableThumbprint(object item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var builder = GetNullableThumbprintBuilder(item.GetType());
            return builder.GetNullableThumbprint(item);
        }
    }

    internal sealed class NullableThumbprintBuilder
    {
        private readonly Type _type;
        private static int _globalTypeIndex;
        private readonly long _typeIndex;
        private const int MaxTypeIndex = 1 << 16; // 65536 types max, 16 bits for the type
        private const int MaxProperties = 48;

        private readonly List<INullablePropertyAccessor> _nullableAccessors;


        public NullableThumbprintBuilder(Type type)
        {
            _type = type;

            // Each type gets a unique type index
            _typeIndex = Interlocked.Increment(ref _globalTypeIndex);

            if (_globalTypeIndex > MaxTypeIndex)
            {
                throw new InvalidOperationException("The maximum number of compiled queries was reached");
            }

            foreach (var propertyInfo in _type.GetProperties())
            {
                var propertyType = propertyInfo.PropertyType;

                // Is it a nullable type ?
                if (propertyType != typeof(string)
                    && Nullable.GetUnderlyingType(propertyType) == null)
                {
                    continue;
                }

                _nullableAccessors ??= new List<INullablePropertyAccessor>();

                if (_nullableAccessors.Count >= MaxProperties)
                {
                    throw new InvalidOperationException("The maximum number of nullable properties was reached for " + _type.FullName);
                }

                var getType = typeof(Func<,>).MakeGenericType(new[] { _type, propertyType });
                var getter = propertyInfo.GetGetMethod().CreateDelegate(getType);
                var accessorType = typeof(NullableAccessor<,>).MakeGenericType(_type, propertyType);

                _nullableAccessors.Add(Activator.CreateInstance(accessorType, new object[] { getter }) as INullablePropertyAccessor);
            }
        }

        private interface INullablePropertyAccessor
        {
            bool IsPropertyNull(object obj);
        }

        private sealed class NullableAccessor<T, TU> : INullablePropertyAccessor where T : class
        {
            private readonly Func<T, TU> _getter;

            public NullableAccessor(Func<T, TU> getter)
            {
                _getter = getter;
            }

            bool INullablePropertyAccessor.IsPropertyNull(object obj)
            {
                return _getter((T)obj) is null;
            }
        }

        private const long long1 = 1;

        /// <summary>
        /// Returns an 64 bits integer representing the unique set of nullable fields as a bit mask. The 16 MSB represent the type, and the 48 LSB represent individual fields
        /// </summary>
        public long GetNullableThumbprint(object o)
        {
            var mask = _typeIndex << MaxProperties;

            if (_nullableAccessors == null)
            {
                return mask;
            }

            for (var i = 0; i < _nullableAccessors.Count; i++)
            {
                if (_nullableAccessors[i].IsPropertyNull(o))
                {
                    mask = mask | (long1 << i);
                }
            }

            return mask;
        }
    }
}
