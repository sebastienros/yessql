namespace YesSql
{
    public interface ISqlFunction
    {
        string Render(string[] arguments);
    }
}
