namespace YesSql.Core.Sharding
{
    public class ShardStrategyImpl : IShardStrategy
    {
        public ShardStrategyImpl(IShardSelectionStrategy shardSelectionStrategy)
        {
            ShardSelectionStrategy = shardSelectionStrategy;
        }

        public IShardSelectionStrategy ShardSelectionStrategy { get; private set; }
    }
}
