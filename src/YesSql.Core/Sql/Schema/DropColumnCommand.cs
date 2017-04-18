namespace YesSql.Sql.Schema
{
    public class DropColumnCommand : ColumnCommand
    {
        public DropColumnCommand(string tableName, string columnName)
            : base(tableName, columnName)
        {
        }
    }
}
