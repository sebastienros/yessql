using System;
using System.Linq.Expressions;
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

            Type accessorType;
            Type getType;
            Type setType;
            Delegate getter;
            Delegate setter;

            if (typeof(T) != tProperty && typeof(T) == typeof(long) && tProperty == typeof(int))
            {
                // The entity has "Id" property of type "int". We have to cast it back and forth.

                getType = typeof(Func<,>).MakeGenericType(new[] { tContainer, typeof(long) });
                setType = typeof(Action<,>).MakeGenericType(new[] { tContainer, typeof(long) });

                var entityParamExpression = Expression.Parameter(tContainer, "entity");
                var propertyExpression = Expression.Property(entityParamExpression, propertyName);

                // Convert the property value to long before returning it ( int > long )
                var convertToLongExpression = Expression.Convert(propertyExpression, typeof(long));

                getter = Expression.Lambda(getType, convertToLongExpression, entityParamExpression).Compile();

                // Convert the value to int before assigning it to the property ( long > int )
                var idParamExpression = Expression.Variable(typeof(long), "id");
                var convertToIntExpression = Expression.Convert(idParamExpression, typeof(int));
                var assignExpression = Expression.Assign(propertyExpression, convertToIntExpression);

                setter = Expression.Lambda(setType, assignExpression, entityParamExpression, idParamExpression).Compile();

                accessorType = typeof(IdAccessor<,>).MakeGenericType(tContainer, typeof(long));
            }
            else
            {
                getType = typeof(Func<,>).MakeGenericType(new[] { tContainer, tProperty });
                setType = typeof(Action<,>).MakeGenericType(new[] { tContainer, tProperty });

                getter = propertyInfo.GetGetMethod().CreateDelegate(getType);
                setter = propertyInfo.GetSetMethod(true).CreateDelegate(setType);

                accessorType = typeof(IdAccessor<,>).MakeGenericType(tContainer, tProperty);
            }

            return Activator.CreateInstance(accessorType, new object[] { getter, setter }) as IIdAccessor<T>;
        }

    }
}