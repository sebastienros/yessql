using System;
using System.Data.Common;
using YesSql.Sql.Schema;

namespace YesSql.Sql
{
    /// <summary>
    /// Classes implementing this interface can alter a SQL schema.
    /// </summary>
    public interface ISchemaBuilder
    {
        /// <summary>
        /// Gets the table prefix.
        /// </summary>
        string TablePrefix { get; }

        /// <summary>
        /// Gets the dialect;
        /// </summary>
        ISqlDialect Dialect { get; }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        DbConnection Connection { get; }

        /// <summary>
        /// Gets the transaction.
        /// </summary>
        DbTransaction Transaction { get; }

        /// <summary>
        /// Gets the table name convention.
        /// </summary>
        ITableNameConvention TableNameConvention { get; }

        /// <summary>
        /// Gets whether errors should throw an exception. 
        /// </summary>
        bool ThrowOnError { get; }

        /// <summary>
        /// Alters an existing table.
        /// </summary>
        ISchemaBuilder AlterTable(string name, Action<IAlterTableCommand> table);

        /// <summary>
        /// Alters an index table.
        /// </summary>
        ISchemaBuilder AlterIndexTable(Type indexType, Action<IAlterTableCommand> table, string collection);
        
        /// <summary>
        /// Creates a foreign key.
        /// </summary>
        ISchemaBuilder CreateForeignKey(string name, string srcTable, string[] srcColumns, string destTable, string[] destColumns);
        
        /// <summary>
        /// Creates a Map Index table.
        /// </summary>
        ISchemaBuilder CreateMapIndexTable(Type indexType, Action<ICreateTableCommand> table, string collection);
        
        /// <summary>
        /// Creates a Reduce Index table. 
        /// </summary>
        ISchemaBuilder CreateReduceIndexTable(Type indexType, Action<ICreateTableCommand> table, string collection);
        
        /// <summary>
        /// Creates a table.
        /// </summary>
        ISchemaBuilder CreateTable(string name, Action<ICreateTableCommand> table);
        
        /// <summary>
        /// Removes a foreign key.
        /// </summary>
        ISchemaBuilder DropForeignKey(string srcTable, string name);
        
        /// <summary>
        /// Removes a Map Index table.
        /// </summary>
        ISchemaBuilder DropMapIndexTable(Type indexType, string collection = null);

        /// <summary>
        /// Removes a Reduce Index table.
        /// </summary>
        ISchemaBuilder DropReduceIndexTable(Type indexType, string collection = null);

        /// <summary>
        /// Removes a table.
        /// </summary>
        ISchemaBuilder DropTable(string name);
    }
}
