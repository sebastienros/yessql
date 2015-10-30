using System.Collections.Generic;

namespace YesSql.Core.Indexes
{
    public class ReduceIndex : Index
    {
        public ReduceIndex()
        {
            Documents = new List<Document>();
        }

        public List<Document> RemovedDocuments = new List<Document>();

        private List<Document> Documents { get; set; }

        public override void AddDocument(Document document)
        {
            Documents.Add(document);
        }

        public override void RemoveDocument(Document document)
        {
            Documents.Remove(document);
            RemovedDocuments.Add(document);
        }

        public override IEnumerable<Document> GetAddedDocuments()
        {
            return Documents;
        }

        public override IEnumerable<Document> GetRemovedDocuments()
        {
            return RemovedDocuments;
        }
    }
}