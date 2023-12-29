namespace YesSql;

public class QueryContext
{
    /// <summary>
    /// When enabled, the retrieved items will not be stored in the internal cache.
    /// </summary>
    public bool WithNoTracking { get; set; }
}
