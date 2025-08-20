using System;

namespace YesSql.Sql.Schema
{
    public class CreateTableCommand : SchemaCommand, ICreateTableCommand
    {
        public CreateTableCommand(string name)
            : base(name, SchemaCommandType.CreateTable)
        {
        }

        public ICreateTableCommand Column(string columnName, Type dbType, Action<ICreateColumnCommand> column = null)
        {
            var command = new CreateColumnCommand(Name, columnName);
            command.WithType(dbType);

            column?.Invoke(command);

            TableCommands.Add(command);
            return this;
        }

        public ICreateTableCommand Column<T>(string columnName, Action<ICreateColumnCommand> column = null)
        {
            return Column(columnName, typeof(T), column);
        }

        public ICreateTableCommand Column(IdentityColumnSize identityColumnSize, string columnName, Action<ICreateColumnCommand> column = null)
            => identityColumnSize == IdentityColumnSize.Int32 ? Column<int>(columnName, column) : Column<long>(columnName, column);

    }
}
