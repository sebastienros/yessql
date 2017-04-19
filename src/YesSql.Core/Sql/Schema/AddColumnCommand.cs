namespace YesSql.Sql.Schema
{
    public class AddColumnCommand : CreateColumnCommand, IAddColumnCommand
    {
        public AddColumnCommand(string tableName, string name) : base(tableName, name)
        {
        }
    }
}
