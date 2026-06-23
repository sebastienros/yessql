namespace YesSql.Filters.Services
{
    public interface IFilterParser<TResult>
    {
        TResult Parse(string text);
    }
}
