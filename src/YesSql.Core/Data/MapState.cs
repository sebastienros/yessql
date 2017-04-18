using System.Collections.Generic;
using YesSql.Indexes;

namespace YesSql.Data
{
    public class MapState
    {
        public MapState(IIndex map, MapStates state)
        {
            Map = map;
            State = state;

            RemovedDocuments = new List<Document>();
            AddedDocuments = new List<Document>();
        }

        public IIndex Map { get; set; }
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
