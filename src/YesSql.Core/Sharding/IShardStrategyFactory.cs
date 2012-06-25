using System.Collections.Generic;

namespace YesSql.Core.Sharding
{
    public interface IShardStrategyFactory
    {
        IShardStrategy Create(IEnumerable<string> shardIds);
    }
}
