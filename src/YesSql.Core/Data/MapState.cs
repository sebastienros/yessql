using YesSql.Core.Indexes;

namespace YesSql.Core.Data {
    public class MapState {
        public MapState(IIndex map, MapStates state)
        {
            Map = map;
            State = state;
        }

        public IIndex Map { get; set; }
        public MapStates State { get; set; }
    }

    public enum MapStates
    {
        New,
        Update,
        Delete
    }
}
