using System.Collections.Generic;

namespace YesSql.Indexes
{
    public abstract class MapIndex : IIndex
    {
        private Document Document { get; set; }

        public int Id { get; set; }

        void IIndex.AddDocument(Document document)
        {
            Document = document;
        }

        void IIndex.RemoveDocument(Document document)
        {
            Document = null;
        }

        IEnumerable<Document> IIndex.GetAddedDocuments()
        {
            yield return Document;
        }

        IEnumerable<Document> IIndex.GetRemovedDocuments()
        {
            yield break;
        }
    }
}