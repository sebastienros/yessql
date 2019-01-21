using System.Collections.Generic;

namespace YesSql.Indexes
{
    public class IdentityMap
    {
        private readonly Dictionary<int, object> _documentIds = new Dictionary<int, object>();
        private readonly Dictionary<object, int> _entities = new Dictionary<object, int>();

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

        public void Add(int id, object entity)
        {
            _entities.Add(entity, id);
            _documentIds.Add(id, entity);
        }

        public void Remove(int id, object entity)
        {
            _entities.Remove(entity);
            _documentIds.Remove(id);
        }

        public IEnumerable<object> GetAll()
        {
            return _entities.Keys;
        }

        public void Clear()
        {
            _entities.Clear();
            _documentIds.Clear();
        }
    }
}
