namespace YesSql.Sql.Schema
{
    public interface IDropForeignKeyCommand : ISchemaCommand
    {
        string SrcTable { get; }
    }
}
