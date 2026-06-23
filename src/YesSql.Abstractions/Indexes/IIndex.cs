using System.Collections.Generic;

namespace YesSql.Indexes
{
    /// <summary>
    /// Represents an index entry associated with one or more documents in the store.
    /// </summary>
    public interface IIndex
    {
        /// <summary>
        /// Gets or sets the unique identifier of the index record.
        /// </summary>
        long Id { get; set; }

        /// <summary>
        /// Associates a document with this index.
        /// </summary>
        /// <param name="document">The document to add.</param>
        void AddDocument(Document document);

        /// <summary>
        /// Removes the association between a document and this index.
        /// </summary>
        /// <param name="document">The document to remove.</param>
        void RemoveDocument(Document document);

        /// <summary>
        /// Gets the documents that have been associated with this index.
        /// </summary>
        /// <returns>The added documents.</returns>
        IEnumerable<Document> GetAddedDocuments();

        /// <summary>
        /// Gets the documents that have been removed from this index.
        /// </summary>
        /// <returns>The removed documents.</returns>
        IEnumerable<Document> GetRemovedDocuments();
    }
}
