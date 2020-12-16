using System;
using System.Data;
using System.Linq.Expressions;

namespace YesSql.Sql.Schema
{
    public interface ICreateTableCommand : ISchemaCommand
    {
        ICreateTableCommand Column(string columnName, DbType dbType, Action<ICreateColumnCommand> column = null);
        ICreateTableCommand Column<T>(string columnName, Action<ICreateColumnCommand> column = null);
        ICreateTableCommand Column<TModel>(Expression<Func<TModel, object>> expression, Action<ICreateColumnCommand> column = null);
    }
}
