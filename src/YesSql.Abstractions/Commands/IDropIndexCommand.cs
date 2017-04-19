namespace YesSql.Sql.Schema
{
    public interface IDropIndexCommand : ITableCommand
    {
        string IndexName { get; set; }
    }
}
