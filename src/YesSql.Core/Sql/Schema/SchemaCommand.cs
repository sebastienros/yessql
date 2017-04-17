using System.Collections.Generic;

namespace YesSql.Sql.Schema
{
    public abstract class SchemaCommand : ISchemaCommand
    {
        protected SchemaCommand(string name, SchemaCommandType type)
        {
            TableCommands = new List<TableCommand>();
            Type = type;
            WithName(name);
        }

        public string Name { get; private set; }
        public List<TableCommand> TableCommands { get; private set; }

        public SchemaCommandType Type { get; private set; }

        public SchemaCommand WithName(string name)
        {
            Name = name;
            return this;
        }
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
