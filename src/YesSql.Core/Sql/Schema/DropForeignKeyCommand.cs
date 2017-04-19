namespace YesSql.Sql.Schema
{
    public class DropForeignKeyCommand : SchemaCommand, IDropForeignKeyCommand
    {
        public string SrcTable { get; private set; }

        public DropForeignKeyCommand(string srcTable, string name)
            : base(name, SchemaCommandType.DropForeignKey)
        {
            SrcTable = srcTable;
        }
    }
}
