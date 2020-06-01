namespace YesSql
{
    public class NullNamingPolicy : NamingPolicy
    {
        public override string ConvertName(string name) => name;
    }
}
