using System.Collections.Generic;
using YesSql.Data;
using YesSql.Indexes;

namespace YesSql
{
    internal class SessionState
    {
        public readonly Dictionary<IndexDescriptor, List<MapState>> Maps = new Dictionary<IndexDescriptor, List<MapState>>();

        public readonly IdentityMap IdentityMap = new IdentityMap();

        // entities that need to be created in the next flush
        public readonly HashSet<object> Saved = new HashSet<object>();

        // entities that already exist and need to be updated in the next flush
        public readonly HashSet<object> Updated = new HashSet<object>();

        // entities that are already saved or updated in a previous flush
        public readonly HashSet<object> Tracked = new HashSet<object>();

        // ids of entities that are checked for concurrency
        public readonly HashSet<int> Concurrent = new HashSet<int>();

        // entities that need to be deleted in the next flush
        public readonly HashSet<object> Deleted = new HashSet<object>();

    }
}
