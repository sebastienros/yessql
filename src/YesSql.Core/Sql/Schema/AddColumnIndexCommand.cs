using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YesSql.Sql.Schema
{
    public class AddColumnIndexCommand : ISchemaCommand, IAddColumnIndexCommand
    {
        public string Name { get; }
        public string IndexName { get; }
        public List<ITableCommand> TableCommands { get; }

        public AddColumnIndexCommand(string tableName, string indexName, params Action<ICreateColumnIndexCommand>[] columns)
            //: base(tableName)
        {
            Name = tableName;
            IndexName = indexName;
            TableCommands = new List<ITableCommand>();

            foreach (var column in columns)
            {
                var command = new CreateColumnIndexCommand(tableName, indexName);
                column.Invoke(command);

                TableCommands.Add(command);
            }
        }
    }
}
