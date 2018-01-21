namespace YesSql.Tests
{
    public class InMemoryTestsPrefixed : InMemoryTests
    {
        protected override string TablePrefix => "tp";
    }
}
