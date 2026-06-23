using System.Collections.Generic;

namespace YesSql.Indexes
{
    /// <summary>
    /// A base class for map indexes, which associate a single document with one index record.
    /// </summary>
    public abstract class MapIndex : IIndex
    {
        private Document _document;

        /// <summary>
        /// Gets or sets the unique identifier of the index record.
        /// </summary>
        public long Id { get; set; }

        void IIndex.AddDocument(Document document)
        {
            _document = document;
        }

        void IIndex.RemoveDocument(Document document)
        {
            _document = null;
        }

        IEnumerable<Document> IIndex.GetAddedDocuments()
        {
            yield return _document;
        }

        IEnumerable<Document> IIndex.GetRemovedDocuments()
        {
            yield break;
        }
    }
}