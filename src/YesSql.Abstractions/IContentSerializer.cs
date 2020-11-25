using System;

namespace YesSql
{
    /// <summary>
    /// This interface represents a components capable of serializing and deserializing 
    /// an object.
    /// </summary>
    public interface IContentSerializer
    {
        /// <summary>
        /// Serializes an object into a <see cref="String" />.
        /// </summary>
        /// <param name="item">The object to serialize.</param>
        /// <returns>The serialized object.</returns>
        string Serialize(object item);

        /// <summary>
        /// Deserializes an object from a string.
        /// </summary>
        /// <param name="content">The <see cref="String" /> instance representing the object to deserialize.</param>
        /// <param name="type">The type of the object to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        object Deserialize(string content, Type type);

        /// <summary>
        /// Deserializes an object to a <c>dynamic</c> instance.
        /// </summary>
        /// <param name="content">The <see cref="String" /> instance representing the object to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        dynamic DeserializeDynamic(string content);
    }
}
