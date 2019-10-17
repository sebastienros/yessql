namespace YesSql.Sql.Schema
{
    public class RenameColumnCommand : ColumnCommand, IRenameColumnCommand
    {
        public string NewColumnName { get; }

        public RenameColumnCommand(string tableName, string columnName, string newColumnName): base(tableName, columnName)
        {
            NewColumnName = newColumnName;
        }
    }
}
