using System;
using System.Reflection.Metadata;

namespace YesSql
{
    public class ConcurrencyException : Exception
    {
        private readonly string _message;

        public Document Document { get; }

        public ConcurrencyException(Document document)
        {
            Document = document;

            _message= $"""
The document could not be updated as it has been changed by another process.
     ID: {Document.Id}
   Type: {Document.Type}
Version: {Document.Version}
Content: {Document.Content}        
""";
        }
        
        public override string Message { get { return _message; } }
    }
}
