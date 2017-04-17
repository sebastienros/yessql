namespace YesSql.Sql.Schema
{
    public class AddColumnCommand : CreateColumnCommand
    {
        public AddColumnCommand(string tableName, string name) : base(tableName, name)
        {
        }
    }
}
