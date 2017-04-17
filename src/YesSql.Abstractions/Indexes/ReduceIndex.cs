using System.Collections.Generic;

namespace YesSql.Indexes
{
    public class ReduceIndex : IIndex
    {
        public ReduceIndex()
        {
            Documents = new List<Document>();
        }

        public int Id { get; set; }

        List<Document> RemovedDocuments = new List<Document>();

        private List<Document> Documents { get; set; }

        void IIndex.AddDocument(Document document)
        {
            Documents.Add(document);
        }

        void IIndex.RemoveDocument(Document document)
        {
            Documents.Remove(document);
            RemovedDocuments.Add(document);
        }

        IEnumerable<Document> IIndex.GetAddedDocuments()
        {
            return Documents;
        }

        IEnumerable<Document> IIndex.GetRemovedDocuments()
        {
            return RemovedDocuments;
        }
    }
}