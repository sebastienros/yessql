namespace YesSql.Filters.Nodes;

/// <summary>
/// Specifies the kind of quoting that surrounded the value of a <see cref="UnaryNode"/> in the source filter expression.
/// </summary>
public enum OperateNodeQuotes
{
    /// <summary>
    /// The value was not enclosed in quotes.
    /// </summary>
    None,
    /// <summary>
    /// The value was enclosed in double quotes.
    /// </summary>
    Double,
    /// <summary>
    /// The value was enclosed in single quotes.
    /// </summary>
    Single
}