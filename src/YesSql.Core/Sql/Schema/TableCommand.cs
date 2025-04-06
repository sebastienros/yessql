using System.Collections.Generic;

namespace YesSql.Sql.Schema
{
    public abstract class TableCommand : ITableCommand
    {
        public string Name { get; private set; }

        public List<ITableCommand> TableCommands { get; private set; }

        protected TableCommand(string tableName)
        {
            Name = tableName;
        }
    }
}
