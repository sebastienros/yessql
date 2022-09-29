using System;

namespace YesSql.Sql.Schema
{
    public interface ICreateTableCommand : ISchemaCommand
    {
        ICreateTableCommand Column(string columnName, Type dbType, Action<ICreateColumnCommand> column = null);
        ICreateTableCommand Column<T>(string columnName, Action<ICreateColumnCommand> column = null);
        public ICreateTableCommand Column(bool useInt64Type, string columnName, Action<ICreateColumnCommand> column = null) => useInt64Type ? Column<int>(columnName, column) : Column<long>(columnName, column);
    }
}
