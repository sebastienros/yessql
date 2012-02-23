using System;

namespace YesSql.Core.Data {
    public interface IIdAccessor
    {
        object Get(object obj);
        void Set(object obj, object value);

    }

    public class IdAccessor : IIdAccessor
    {
        readonly Func<object, object> _getter;
        readonly Action<object, object> _setter;

        public IdAccessor(Func<object, object> getter, Action<object, object> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        object IIdAccessor.Get(object obj)
        {
            return _getter(obj);
        }

        void IIdAccessor.Set(object obj, object value)
        {
            _setter(obj, value);
        }
    }
}
