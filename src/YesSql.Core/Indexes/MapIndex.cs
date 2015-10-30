using System.Collections.Generic;

namespace YesSql.Core.Indexes
{
    public abstract class MapIndex : Index 
    {
        private Document Document { get; set; }
        public override void AddDocument(Document document)
        {
            Document = document;
        }

        public override void RemoveDocument(Document document)
        {
            Document = null;
        }

        public override IEnumerable<Document> GetAddedDocuments()
        {
            yield return Document;
        }

        public override IEnumerable<Document> GetRemovedDocuments()
        {
            yield break;
        }
    }
}