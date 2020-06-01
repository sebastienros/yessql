namespace YesSql
{
    public abstract class NamingPolicy
    {
        public static NamingPolicy DefaultCase => new NullNamingPolicy();

        public abstract string ConvertName(string name);
    }
}
