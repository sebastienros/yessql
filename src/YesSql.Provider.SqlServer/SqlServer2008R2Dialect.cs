namespace YesSql.Provider.SqlServer
{
    public class SqlServer2008Dialect : SqlServerDialect
    {
        public override string Name => "SqlServer 2008 R2";

        public override void Page(ISqlBuilder sqlBuilder, string offset, string limit)
        {
            if (limit != null)
            {
                var selector = sqlBuilder.GetSelector();
                selector = " top " + limit + " " + selector;
                sqlBuilder.Selector(selector);
            }

            if (offset != null)
            {
                sqlBuilder.WhereAnd("RowNum >" + offset);
            }
        }
    }
}
