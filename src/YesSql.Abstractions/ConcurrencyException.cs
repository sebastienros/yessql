using System;

namespace YesSql
{
    public class ConcurrencyException : Exception
    {
        public readonly Document Document { get; }

        public ConcurrencyException(Document document) : base($"The document with ID '{document.Id}' and type '{document.Type}' could not be updated as it has been changed by another process.")
        {
            Document = document;
        }
    }
}
