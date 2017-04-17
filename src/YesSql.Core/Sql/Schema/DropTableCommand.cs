namespace YesSql.Sql.Schema
{
    public class DropTableCommand : SchemaCommand
    {
        public DropTableCommand(string name)
            : base(name, SchemaCommandType.DropTable)
        {
        }
    }
}
