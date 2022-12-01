using System.Collections.Generic;
using YesSql.Data;
using YesSql.Indexes;

namespace YesSql
{
    internal class SessionState
    {
        internal Dictionary<IndexDescriptor, List<MapState>> _maps;
        public Dictionary<IndexDescriptor, List<MapState>> Maps => _maps ??= new Dictionary<IndexDescriptor, List<MapState>>();

        internal IdentityMap _identityMap;
        public IdentityMap IdentityMap => _identityMap ??= new IdentityMap();

        // entities that need to be created in the next flush
        internal HashSet<object> _saved;
        public HashSet<object> Saved => _saved ??= new HashSet<object>();

        // entities that already exist and need to be updated in the next flush
        internal HashSet<object> _updated;
        public HashSet<object> Updated => _updated ??= new HashSet<object>();

        // entities that are already saved or updated in a previous flush
        internal HashSet<object> _tracked;
        public HashSet<object> Tracked => _tracked ??= new HashSet<object>();

        // ids of entities that are checked for concurrency
        internal HashSet<long> _concurrent;
        public HashSet<long> Concurrent => _concurrent ??= new HashSet<long>();

        // entities that need to be deleted in the next flush
        internal HashSet<object> _deleted;
        public HashSet<object> Deleted => _deleted ??= new HashSet<object>();
    }
}
