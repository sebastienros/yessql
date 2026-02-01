namespace YesSql;

public static class SqlBuilderExtensions
{
    public static void InnerJoin(this ISqlBuilder builder, string table, string onTable, string onColumn, string toTable, string toColumn, string schema, string alias = null, string toAlias = null, bool onTableIsAlias = false)
        => builder.Join(JoinType.Inner, table, onTable, onColumn, toTable, toColumn, schema, alias, toAlias, onTableIsAlias);

    public static void LeftJoin(this ISqlBuilder builder, string table, string onTable, string onColumn, string toTable, string toColumn, string schema, string alias = null, string toAlias = null, bool onTableIsAlias = false)
        => builder.Join(JoinType.Left, table, onTable, onColumn, toTable, toColumn, schema, alias, toAlias, onTableIsAlias);

    public static void RightJoin(this ISqlBuilder builder, string table, string onTable, string onColumn, string toTable, string toColumn, string schema, string alias = null, string toAlias = null, bool onTableIsAlias = false)
        => builder.Join(JoinType.Right, table, onTable, onColumn, toTable, toColumn, schema, alias, toAlias, onTableIsAlias);
}
