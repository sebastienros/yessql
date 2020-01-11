using System;
using System.Data;

namespace YesSql.Sql.Schema
{
    public interface IAlterTableCommand : ISchemaCommand
    {
        void AddColumn(string columnName, DbType dbType, Action<IAddColumnCommand> column = null);
        void AddColumn<T>(string columnName, Action<IAddColumnCommand> column = null);
        void AlterColumn(string columnName, Action<IAlterColumnCommand> column = null);
        void RenameColumn(string columnName, string newName);
        void DropColumn(string columnName);
        void CreateIndex(string indexName, params string[] columnNames);
        void DropIndex(string indexName);
    }
}
