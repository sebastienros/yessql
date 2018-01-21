namespace YesSql.Tests
{
    public class MySqlTestsPrefixed : MySqlTests
    {
        protected override string TablePrefix => "tp";
    }
}
