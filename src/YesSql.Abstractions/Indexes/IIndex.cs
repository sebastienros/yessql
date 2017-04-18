using System.Collections.Generic;

namespace YesSql.Indexes
{
    public interface IIndex
    {
        int Id { get; set; }
        void AddDocument(Document document);
        void RemoveDocument(Document document);
        IEnumerable<Document> GetAddedDocuments();
        IEnumerable<Document> GetRemovedDocuments();
    }
}
