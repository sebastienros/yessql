namespace YesSql.Sql.Schema
{
    public class DropIndexCommand : TableCommand, IDropIndexCommand
    {
        public string IndexName { get; set; }

        public DropIndexCommand(string tableName, string indexName)
            : base(tableName)
        {
            IndexName = indexName;
        }
    }
}
