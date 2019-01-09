using System.Collections.Generic;

namespace YesSql.Indexes
{
    public class IdentityMap
    {
        private readonly IDictionary<long, object> _documentIds = new Dictionary<long, object>();
        private readonly IDictionary<object, long> _entities = new Dictionary<object, long>();

        public bool TryGetDocumentId(object item, out long id)
        {
            return _entities.TryGetValue(item, out id);
        }

        public bool TryGetEntityById(long id, out object document)
        {
            return _documentIds.TryGetValue(id, out document);
        }

        public bool HasEntity(object entity)
        {
            return _entities.ContainsKey(entity);
        }

        public void Add(long id, object entity)
        {
            _entities.Add(entity, id);
            _documentIds.Add(id, entity);
        }

        public void Remove(long id, object entity)
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
