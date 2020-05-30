namespace YesSql
{
    public abstract class NamingPolicy
    {
        public static NamingPolicy PascalCase => new PascalCaseNamingPolicy();

        public abstract string ConvertName(string name);
    }
}
