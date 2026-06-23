namespace YesSql;

/// <summary>
/// Defines the type of join used when combining index tables in a query.
/// </summary>
public enum JoinType
{
    /// <summary>
    /// An inner join, returning only rows that match in both tables.
    /// </summary>
    Inner = 0,

    /// <summary>
    /// A left outer join, returning all rows from the left table.
    /// </summary>
    Left = 1,

    /// <summary>
    /// A right outer join, returning all rows from the right table.
    /// </summary>
    Right = 2
}
