using System.Collections.Generic;

namespace YesSql.Indexes
{
    /// <summary>
    /// A base class for reduce indexes, which aggregate multiple documents into a single index record.
    /// </summary>
    public class ReduceIndex : IIndex
    {
        private readonly List<Document> _removedDocuments = new();
        private readonly List<Document> _documents = new();
        
        /// <summary>
        /// Gets or sets the unique identifier of the index record.
        /// </summary>
        public long Id { get; set; }

        void IIndex.AddDocument(Document document)
        {
            _documents.Add(document);
        }

        void IIndex.RemoveDocument(Document document)
        {
            _documents.Remove(document);
            _removedDocuments.Add(document);
        }

        IEnumerable<Document> IIndex.GetAddedDocuments() => _documents;

        IEnumerable<Document> IIndex.GetRemovedDocuments() => _removedDocuments;
    }
}