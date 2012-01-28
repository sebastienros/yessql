namespace YesSql.Core.Sharding
{
    public interface IShardStrategy
    {
        IShardSelectionStrategy ShardSelectionStrategy { get; }
    }
}