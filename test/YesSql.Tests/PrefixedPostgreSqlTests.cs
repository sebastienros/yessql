namespace YesSql.Tests
{
    public class PrefixedPostgreSqlTests : PostgreSqlTests
    {
        protected override string TablePrefix => "tp";
    }
}
