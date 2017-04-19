using System.Collections.Generic;

namespace YesSql.Sql.Schema
{
    public abstract class SchemaCommand : ISchemaCommand
    {
        protected SchemaCommand(string name, SchemaCommandType type)
        {
            TableCommands = new List<ITableCommand>();
            Type = type;
            WithName(name);
        }

        public string Name { get; private set; }
        public List<ITableCommand> TableCommands { get; private set; }

        public SchemaCommandType Type { get; private set; }

        public ISchemaCommand WithName(string name)
        {
            Name = name;
            return this;
        }
    }
}
