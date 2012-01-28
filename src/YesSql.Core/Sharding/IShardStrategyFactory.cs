using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YesSql.Core.Sharding
{
    public interface IShardStrategyFactory
    {
        IShardStrategy Create(IEnumerable<string> shardIds);
    }
}
