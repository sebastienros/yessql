namespace YesSql.Tests
{
    public class SqlServerTestsPrefixed : SqlServerTests
    {
        protected override string TablePrefix => "tp";
    }
}
