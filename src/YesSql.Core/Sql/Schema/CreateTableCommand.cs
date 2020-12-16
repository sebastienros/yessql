using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using YesSql.Utils;

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


        public ICreateTableCommand Column<TModel>(Expression<Func<TModel, object>> expression, Action<ICreateColumnCommand> column = null)
        {
            PropertyInfo property = ReflectionHelpers.FromExpression(expression);

            if (property == null)
            {
                throw new ArgumentException($"The lambda expression should point to a valid Property.");
            }

            return Column(property.Name, _dialect.GetDbType(property.PropertyType), column);
        }

    }
}
