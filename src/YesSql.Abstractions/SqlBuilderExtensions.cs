namespace YesSql;

/// <summary>
/// Provides extension methods for adding joins to an <see cref="ISqlBuilder"/>.
/// </summary>
public static class SqlBuilderExtensions
{
    /// <summary>
    /// Adds an inner join between two tables to the query.
    /// </summary>
    /// <param name="builder">The SQL builder to add the join to.</param>
    /// <param name="table">The name of the table to join.</param>
    /// <param name="onTable">The name of the table that holds the join column.</param>
    /// <param name="onColumn">The name of the column on the joined table.</param>
    /// <param name="toTable">The name of the table being joined to.</param>
    /// <param name="toColumn">The name of the column on the table being joined to.</param>
    /// <param name="schema">The schema the tables belong to.</param>
    /// <param name="alias">An optional alias for the joined table.</param>
    /// <param name="toAlias">An optional alias for the table being joined to.</param>
    public static void InnerJoin(this ISqlBuilder builder, string table, string onTable, string onColumn, string toTable, string toColumn, string schema, string alias = null, string toAlias = null)
        => builder.Join(JoinType.Inner, table, onTable, onColumn, toTable, toColumn, schema, alias, toAlias);

    /// <summary>
    /// Adds a left outer join between two tables to the query.
    /// </summary>
    /// <param name="builder">The SQL builder to add the join to.</param>
    /// <param name="table">The name of the table to join.</param>
    /// <param name="onTable">The name of the table that holds the join column.</param>
    /// <param name="onColumn">The name of the column on the joined table.</param>
    /// <param name="toTable">The name of the table being joined to.</param>
    /// <param name="toColumn">The name of the column on the table being joined to.</param>
    /// <param name="schema">The schema the tables belong to.</param>
    /// <param name="alias">An optional alias for the joined table.</param>
    /// <param name="toAlias">An optional alias for the table being joined to.</param>
    public static void LeftJoin(this ISqlBuilder builder, string table, string onTable, string onColumn, string toTable, string toColumn, string schema, string alias = null, string toAlias = null)
        => builder.Join(JoinType.Left, table, onTable, onColumn, toTable, toColumn, schema, alias, toAlias);

    /// <summary>
    /// Adds a right outer join between two tables to the query.
    /// </summary>
    /// <param name="builder">The SQL builder to add the join to.</param>
    /// <param name="table">The name of the table to join.</param>
    /// <param name="onTable">The name of the table that holds the join column.</param>
    /// <param name="onColumn">The name of the column on the joined table.</param>
    /// <param name="toTable">The name of the table being joined to.</param>
    /// <param name="toColumn">The name of the column on the table being joined to.</param>
    /// <param name="schema">The schema the tables belong to.</param>
    /// <param name="alias">An optional alias for the joined table.</param>
    /// <param name="toAlias">An optional alias for the table being joined to.</param>
    public static void RightJoin(this ISqlBuilder builder, string table, string onTable, string onColumn, string toTable, string toColumn, string schema, string alias = null, string toAlias = null)
        => builder.Join(JoinType.Right, table, onTable, onColumn, toTable, toColumn, schema, alias, toAlias);
}
