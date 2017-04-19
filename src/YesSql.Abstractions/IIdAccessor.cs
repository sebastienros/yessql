namespace YesSql
{
    /// <summary>
    /// An implementation of this interface provides the accessors for the identifier of a 
    /// document instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IIdAccessor<T>
    {
        /// <summary>
        /// Gets the identifier value of an object.
        /// </summary>
        /// <param name="obj">The object to get the identifier from.</param>
        /// <returns>The identifier of the object.</returns>
        T Get(object obj);

        /// <summary>
        /// Sets the identifier value of an object.
        /// </summary>
        /// <param name="obj">The object to set the identifier to.</param>
        /// <param name="value">The identifier to set.</param>
        void Set(object obj, T value);
    }
}
