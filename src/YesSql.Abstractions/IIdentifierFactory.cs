using System;

namespace YesSql
{
    /// <summary>
    /// This interface represents a component which can create
    /// and instance of <see cref="IIdAccessor"/> in order to 
    /// get/set the identifier of an object
    /// </summary>
    public interface IIdentifierFactory
    {
        IIdAccessor<T> CreateAccessor<T>(Type tContainer, string name);
    }
}