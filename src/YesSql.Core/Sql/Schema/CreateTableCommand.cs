using System;
using System.Data;

namespace YesSql.Sql.Schema
{
    public class CreateTableCommand : SchemaCommand, ICreateTableCommand
    {
        private readonly ISqlDialect _dialect;

        public CreateTableCommand(string name, ISqlDialect dialect)
            : base(name, SchemaCommandType.CreateTable)
        {
            _dialect = dialect;
        }

        public ICreateTableCommand Column(string columnName, DbType dbType, Action<ICreateColumnCommand> column = null)
        {
            var command = new CreateColumnCommand(Name, columnName);
            command.WithType(dbType);

            column?.Invoke(command);

            TableCommands.Add(command);
            return this;
        }

        public ICreateTableCommand Column<T>(string columnName, Action<ICreateColumnCommand> column = null)
        {
            DbType dbType = _dialect.GetDbType(typeof(T));

            return Column(columnName, dbType, column);
        }

    }
}
