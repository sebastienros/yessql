using System;

namespace YesSql
{
    /// <summary>
    /// This interface represents a component which can create
    /// an instance of <see cref="IAccessor{T}"/> in order to 
    /// get or set a specific value of an object.
    /// </summary>
    public interface IAccessorFactory
    {
        /// <summary>
        /// Creates an <see cref="IAccessor{T}" /> instance.
        /// </summary>
        IAccessor<T> CreateAccessor<T>(Type tContainer);
    }
}
