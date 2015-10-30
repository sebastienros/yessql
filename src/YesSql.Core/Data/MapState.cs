using System.Collections.Generic;
using YesSql.Core.Indexes;

namespace YesSql.Core.Data {
    public class MapState {
        public MapState(Index map, MapStates state)
        {
            Map = map;
            State = state;

            RemovedDocuments = new List<Document>();
            AddedDocuments = new List<Document>();
        }

        public Index Map { get; set; }
        public MapStates State { get; set; }
        public List<Document> RemovedDocuments { get; }
        public List<Document> AddedDocuments { get; }
    }

    public enum MapStates
    {
        New,
        Update,
        Delete
    }
}
