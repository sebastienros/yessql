using System;
using System.Data;

namespace YesSql.Sql.Schema
{
    public interface ICreateTableCommand : ISchemaCommand
    {
        ICreateTableCommand Column(string columnName, DbType dbType, Action<ICreateColumnCommand> column = null);
        ICreateTableCommand Column<T>(string columnName, Action<ICreateColumnCommand> column = null);
    }
}
