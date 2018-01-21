namespace YesSql.Tests
{
    public class SqliteTestsPrefixed : SqliteTests
    {
        protected override string TablePrefix => "tp";
    }
}
