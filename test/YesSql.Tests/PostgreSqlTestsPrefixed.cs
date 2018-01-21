namespace YesSql.Tests
{
    public class PostgreSqlTestsPrefixed : PostgreSqlTests
    {
        protected override string TablePrefix => "tp";
    }
}
