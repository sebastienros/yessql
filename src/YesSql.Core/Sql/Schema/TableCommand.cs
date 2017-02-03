namespace YesSql.Core.Sql.Schema
{
    public class TableCommand : ISchemaCommand
    {
        public string TableName { get; private set; }

        public TableCommand(string tableName)
        {
            TableName = tableName;
        }

    }
}
