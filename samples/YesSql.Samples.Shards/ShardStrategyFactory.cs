using System.Collections.Generic;
using System.Linq;
using YesSql.Core.Sharding;
using YesSql.Samples.Shards.Models;

namespace YesSql.Samples.Shards {
    public class ShardStrategyFactory : IShardStrategyFactory
    {
        public IShardStrategy Create(IEnumerable<string> shardIds)
        {
            var pss = new IndexSelectionStategy(shardIds);
            return new ShardStrategyImpl(pss);
        }
    }

    public class IndexSelectionStategy : IShardSelectionStrategy {
        private readonly string[] _shardIds;
        
        public IndexSelectionStategy(IEnumerable<string> shardIds)
        {
            _shardIds = shardIds.ToArray();
        }

        public string Select(object obj) {
            if(obj is Product)
            {
                return _shardIds[0];
            }

            if(obj is Order)
            {
                return _shardIds[1];
            }

            return _shardIds[0];

            // return _shardIds[obj.GetHashCode() % _shardIds.Length];
        }
    }

}
