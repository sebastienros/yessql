using System;

namespace YesSql.Sql.Schema
{
    public interface ICreateTableCommand : ISchemaCommand
    {
        ICreateTableCommand Column(string columnName, Type dbType, Action<ICreateColumnCommand> column = null);
        ICreateTableCommand Column<T>(string columnName, Action<ICreateColumnCommand> column = null);
        public ICreateTableCommand Column(IdentityColumnSize identityColumnSize, string columnName, Action<ICreateColumnCommand> column = null) => identityColumnSize == IdentityColumnSize.Int32 ? Column<int>(columnName, column) : Column<long>(columnName, column);
    }
}
