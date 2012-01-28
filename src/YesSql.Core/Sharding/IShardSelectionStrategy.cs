namespace YesSql.Core.Sharding {
    public interface IShardSelectionStrategy {
        string Select(object obj);
    }
}
