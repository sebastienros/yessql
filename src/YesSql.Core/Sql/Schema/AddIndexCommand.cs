namespace YesSql.Sql.Schema
{
    public class AddIndexCommand : TableCommand, IAddIndexCommand
    {
        public string IndexName { get; set; }

        public AddIndexCommand(string tableName, string indexName, params string[] columnNames)
            : base(tableName)
        {
            ColumnNames = columnNames;
            IndexName = indexName;
        }

        public string[] ColumnNames { get; private set; }
    }
}
