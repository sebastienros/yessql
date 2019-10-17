namespace YesSql.Sql.Schema
{
    public interface IRenameColumnCommand : IColumnCommand
    {
        string NewColumnName { get; }
    }
}
