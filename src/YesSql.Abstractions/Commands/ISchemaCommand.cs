using System.Collections.Generic;

namespace YesSql.Sql.Schema
{
    public interface ISchemaCommand
    {
        string Name { get; }
        List<ITableCommand> TableCommands { get; }
    }

    public enum SchemaCommandType
    {
        CreateTable,
        DropTable,
        AlterTable,
        SqlStatement,
        CreateForeignKey,
        DropForeignKey
    }
}
