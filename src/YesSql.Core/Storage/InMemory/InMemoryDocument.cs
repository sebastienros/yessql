using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YesSql.Core.Storage.InMemory
{
    /// <summary>
    /// Represents a document as stored with an <see cref="InMemoryDocumentStorage"/> instance
    /// </summary>
    public class InMemoryDocument
    {
        /// <summary>
        /// The unique identifier of the document.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// the serialized JSON content of the document.
        /// </summary>
        public string Content { get; set; }
    }
}
