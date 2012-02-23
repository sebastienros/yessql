using System;

namespace YesSql.Core.Data {
    public interface IIdAccessor
    {
        object Get(object obj);
        void Set(object obj, object value);
    }

    public class IdAccessor<T, TU> : IIdAccessor
    {
        private readonly Func<T, TU> _getter;
        private readonly Action<T, TU> _setter;

        public IdAccessor(Func<T, TU> getter, Action<T, TU> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        private TU Get(T obj)
        {
            return _getter(obj);
        }

        private void Set(T obj, TU value)
        {
            _setter(obj, value);
        }

        object IIdAccessor.Get(object obj)
        {
            return _getter((T) obj);
        }

        void IIdAccessor.Set(object obj, object value)
        {
            _setter((T) obj, (TU) value);
        }
    }
}
