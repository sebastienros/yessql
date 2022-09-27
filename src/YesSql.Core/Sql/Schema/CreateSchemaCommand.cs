namespace YesSql.Sql.Schema
{
    public class CreateSchemaCommand : SchemaCommand, ICreateSchemaCommand
    {
        public CreateSchemaCommand(string name)
            : base(name, SchemaCommandType.CreateSchema)
        {
        }
    }
}
