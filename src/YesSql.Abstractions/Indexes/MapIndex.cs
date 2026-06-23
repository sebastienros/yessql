using System.Collections.Generic;

namespace YesSql.Indexes
{
    public abstract class MapIndex : IIndex
    {
        private Document _document;

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