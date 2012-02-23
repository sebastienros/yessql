using System;

namespace YesSql.Core.Data
{
    public class DefaultIdentifierFactory : IIdentifierFactory
    {
        public IIdAccessor CreateAccessor(Type tContainer, string name)
        {
            var propertyName = name;
            var propertyInfo = tContainer.GetProperty(propertyName);

            if (propertyInfo == null)
            {
                return new IdAccessor<object, object>(x => null, (x, y) => { });
            }

            var tProperty = propertyInfo.PropertyType;

            var getType = typeof (Func<,>).MakeGenericType(new[] {tContainer, tProperty});
            var setType = typeof (Action<,>).MakeGenericType(new[] {tContainer, tProperty});

            var getter = Delegate.CreateDelegate(getType, propertyInfo.GetGetMethod(), false);
            var setter = Delegate.CreateDelegate(setType, propertyInfo.GetSetMethod(), false);

            var accessorType = typeof (IdAccessor<,>).MakeGenericType(tContainer, tProperty);

            return Activator.CreateInstance(accessorType, new object[] {getter, setter}) as IIdAccessor;
        }

    }
}