using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using YesSql.Utils;

namespace YesSql.Sql.Schema
{
    public class AlterTableCommand : SchemaCommand, IAlterTableCommand
    {
        private readonly ISqlDialect _dialect;
        private readonly string _tablePrefix;

        public AlterTableCommand(string name, ISqlDialect dialect, string tablePrefix)
            : base(name, SchemaCommandType.AlterTable)
        {
            _dialect = dialect;
            _tablePrefix = tablePrefix;
        }

        public void AddColumn(string columnName, DbType dbType, Action<IAddColumnCommand> column = null)
        {
            var command = new AddColumnCommand(Name, columnName);
            command.WithType(dbType);

            column?.Invoke(command);

            TableCommands.Add(command);
        }

        public void AddColumn<TModel>(Expression<Func<TModel, object>> expression, Action<IAddColumnCommand> column = null)
        {
            PropertyInfo property = ReflectionHelpers.FromExpression(expression);

            if (property == null)
            {
                throw new ArgumentException($"The lambda expression should point to a valid Property.");
            }

            AddColumn(property.Name, _dialect.GetDbType(property.PropertyType), column);
        }

        public void AddColumn<T>(string columnName, Action<IAddColumnCommand> column = null)
        {
            DbType dbType = _dialect.GetDbType(typeof(T));

            AddColumn(columnName, dbType, column);
        }

        public void DropColumn(string columnName)
        {
            var command = new DropColumnCommand(Name, columnName);
            TableCommands.Add(command);
        }

        public void AlterColumn(string columnName, Action<IAlterColumnCommand> column = null)
        {
            var command = new AlterColumnCommand(Name, columnName);

            column?.Invoke(command);

            TableCommands.Add(command);
        }

        public void RenameColumn(string columnName, string newColumnName)
        {
            var command = new RenameColumnCommand(Name, columnName, newColumnName);

            TableCommands.Add(command);
        }


        public void CreateIndex(string indexName, params string[] columnNames)
        {
            if (_dialect.PrefixIndex)
            {
                indexName = _tablePrefix + indexName;
            }

            var command = new AddIndexCommand(Name, indexName, columnNames);
            TableCommands.Add(command);
        }

        public void DropIndex(string indexName)
        {
            if (_dialect.PrefixIndex)
            {
                indexName = _tablePrefix + indexName;
            }

            var command = new DropIndexCommand(Name, indexName);
            TableCommands.Add(command);
        }
    }
}
