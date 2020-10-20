using System;

namespace YesSql
{
    /// <summary>
    /// This interface represents a component which can create
    /// and instance of <see cref="IAccessor{T}"/> in order to 
    /// get/set a value of an object
    /// </summary>
    public interface IAccessorFactory
    {
        IAccessor<T> CreateAccessor<T>(Type tContainer);
    }
}