using System.Collections.Generic;

namespace YesSql.Indexes
{
    public class IdentityMap
    {
        private readonly Dictionary<int, object> _documentIds = new Dictionary<int, object>();
        private readonly Dictionary<object, int> _entities = new Dictionary<object, int>();
        private readonly Dictionary<int, Document> _documents = new Dictionary<int, Document>();

        public bool TryGetDocumentId(object item, out int id)
        {
            return _entities.TryGetValue(item, out id);
        }

        public bool TryGetEntityById(int id, out object document)
        {
            return _documentIds.TryGetValue(id, out document);
        }

        public bool HasEntity(object entity)
        {
            return _entities.ContainsKey(entity);
        }

        public void AddEntity(int id, object entity)
        {
            _entities.Add(entity, id);
            _documentIds.Add(id, entity);
        }

        public void AddDocument(Document doc)
        {
            _documents[doc.Id] = doc;
        }

        public bool TryGetDocument(int id, out Document doc)
        {
            return _documents.TryGetValue(id, out doc);
        }

        public void Remove(int id, object entity)
        {
            _entities.Remove(entity);
            _documentIds.Remove(id);
            _documents.Remove(id);
        }

        public IEnumerable<object> GetAll()
        {
            return _entities.Keys;
        }

        public void Clear()
        {
            _entities.Clear();
            _documentIds.Clear();
            _documents.Clear();
        }
    }
}
