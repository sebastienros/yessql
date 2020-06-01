using System.Collections.Generic;
using System.Linq;

namespace YesSql.Indexes
{
    public class IdentityMap
    {
        class Maps
        {
            internal readonly Dictionary<int, object> DocumentIds = new Dictionary<int, object>();
            internal readonly Dictionary<object, int> Entities = new Dictionary<object, int>();
            internal readonly Dictionary<int, Document> Documents = new Dictionary<int, Document>();
        }

        private readonly Dictionary<string, Maps> _collectionMaps = new Dictionary<string, Maps>();

        public bool TryGetDocumentId(string collectionName, object item, out int id)
        {
            if (!_collectionMaps.TryGetValue(collectionName, out var maps))
            {
                id = 0;
                return false;
            }
            return maps.Entities.TryGetValue(item, out id);
        }

        public bool TryGetEntityById(string collectionName, int id, out object document)
        {
            if (!_collectionMaps.TryGetValue(collectionName, out var maps))
            {
                document = null;
                return false;
            }
            return maps.DocumentIds.TryGetValue( id, out document);
        }

        public bool HasEntity(string collectionName, object entity)
        {
            if (!_collectionMaps.TryGetValue(collectionName, out var maps))
            {
                entity = null;
                return false;
            }
            return maps.Entities.ContainsKey(entity);
        }

        public void AddEntity(string collectionName, int id, object entity)
        {
            if (!_collectionMaps.TryGetValue(collectionName, out var maps))
            {
                maps = new Maps();
                _collectionMaps.Add(collectionName, maps);
            }
            maps.Entities.Add(entity, id);
            maps.DocumentIds.Add(id, entity);
        }

        public void AddDocument(string collectionName, Document doc)
        {
            if (!_collectionMaps.TryGetValue(collectionName, value: out var maps))
            {
                maps = new Maps();
                _collectionMaps.Add(collectionName, maps);
            }
            maps.Documents[doc.Id] = doc;
        }

        public bool TryGetDocument(string collectionName, int id, out Document doc)
        {
            if (!_collectionMaps.TryGetValue(collectionName, out var maps))
            {
                doc = null;
                return false;
            }
            return maps.Documents.TryGetValue(id, out doc);
        }

        public void Remove(string collectionName, int id, object entity)
        {
            if (!_collectionMaps.TryGetValue(collectionName, out var maps))
            {
                return;
            }
            maps.Entities.Remove(entity);
            maps.DocumentIds.Remove(id);
            maps.Documents.Remove(id);
        }

        public IEnumerable<object> GetAll(string collectionName)
        {
            if (!_collectionMaps.TryGetValue(collectionName, out var maps))
            {
                return Enumerable.Empty<object>();
            }
            return maps.Entities.Keys;
        }

        public void Clear()
        {
            foreach(var maps in _collectionMaps.Values)
            {
                maps.Entities.Clear();
                maps.DocumentIds.Clear();
                maps.Documents.Clear();
            }
        }
    }
}
