using System;
using System.Reflection.Metadata;

namespace YesSql
{
    public class ConcurrencyException : Exception
    {
        public Document Document { get; }

        public ConcurrencyException(Document document) : base($"The document with Id '{document.Id}' and type '{document.Type}' could not be updated as it has been changed by another process.") => 
            Document = document;
    }
}
