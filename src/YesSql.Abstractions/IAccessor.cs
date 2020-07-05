namespace YesSql
{
    /// <summary>
    /// An implementation of this interface provides the accessors for the identifier of a 
    /// document instance.
    /// </summary>
    /// <typeparam name="T">The type of the value to get and set.</typeparam>
    public interface IAccessor<T>
    {
        /// <summary>
        /// Gets a value of an object.
        /// </summary>
        /// <param name="obj">The object to get the value from.</param>
        /// <returns>The value of the object.</returns>
        T Get(object obj);

        /// <summary>
        /// Sets a value of an object.
        /// </summary>
        /// <param name="obj">The object to set the value to.</param>
        /// <param name="value">The value to set.</param>
        void Set(object obj, T value);
    }
}
