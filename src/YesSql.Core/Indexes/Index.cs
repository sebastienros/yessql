using System.Collections.Generic;

namespace YesSql.Core.Indexes
{
    public abstract class Index
    {
        public int Id { get; set; }
        public abstract void AddDocument(Document document);
        public abstract void RemoveDocument(Document document);
        public abstract IEnumerable<Document> GetAddedDocuments();
        public abstract IEnumerable<Document> GetRemovedDocuments();
    }
}
