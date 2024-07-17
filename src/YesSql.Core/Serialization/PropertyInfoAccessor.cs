using System;
using System.Reflection;

namespace YesSql.Serialization
{
    public class PropertyInfoAccessor
    {
        private readonly IInvoker _invoker;

        public PropertyInfoAccessor(PropertyInfo propertyInfo)
        {
            var delegateType = typeof(Func<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            var d = propertyInfo.GetGetMethod().CreateDelegate(delegateType);

            var invokerType = typeof(Invoker<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            _invoker = Activator.CreateInstance(invokerType, new object[] { d }) as IInvoker;
        }

        public object Get(object obj) => _invoker.Invoke(obj);

        private interface IInvoker
        {
            object Invoke(object target);
        }

        private sealed class Invoker<T, TResult> : IInvoker
        {
            private readonly Func<T, TResult> _d;

            public Invoker(Delegate d)
            {
                _d = (Func<T, TResult>)d;
            }

            public object Invoke(object target) => _d((T)target);
        }
    }
}
