using System;
using System.Reflection.Metadata;

namespace YesSql
{
    /// <summary>
    /// The exception that is thrown when a document could not be updated because it has been
    /// changed concurrently by another process.
    /// </summary>
    public class ConcurrencyException : Exception
    {
        /// <summary>
        /// Gets the document that could not be updated.
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
        /// </summary>
        /// <param name="document">The document that could not be updated.</param>
        public ConcurrencyException(Document document) : base($"The document with Id '{document.Id}' and type '{document.Type}' could not be updated as it has been changed by another process.")
        {
            Document = document;
        }
    }
}
