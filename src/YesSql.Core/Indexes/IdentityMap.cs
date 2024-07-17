using System.Collections.Generic;

namespace YesSql.Indexes
{
    public class IdentityMap
    {
        private readonly Dictionary<long, object> _documentIds = new();
        private readonly Dictionary<object, long> _entities = new();
        private readonly Dictionary<long, Document> _documents = new();

        public bool TryGetDocumentId(object item, out long id) => _entities.TryGetValue(item, out id);

        public bool TryGetEntityById(long id, out object document) => _documentIds.TryGetValue(id, out document);

        public bool HasEntity(object entity) => _entities.ContainsKey(entity);

        public void AddEntity(long id, object entity)
        {
            _entities.Add(entity, id);
            _documentIds.Add(id, entity);
        }

        public void AddDocument(Document doc) => _documents[doc.Id] = doc;

        public bool TryGetDocument(long id, out Document doc) => _documents.TryGetValue(id, out doc);

        public void Remove(long id, object entity)
        {
            _entities.Remove(entity);
            _documentIds.Remove(id);
            _documents.Remove(id);
        }

        public IEnumerable<object> GetAll() => _entities.Keys;

        public void Clear()
        {
            _entities.Clear();
            _documentIds.Clear();
            _documents.Clear();
        }
    }
}
