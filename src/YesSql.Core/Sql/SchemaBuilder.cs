using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using YesSql.Collections;
using YesSql.Sql.Schema;

namespace YesSql.Sql
{
    public class SchemaBuilder : ISchemaBuilder
    {
        private ICommandInterpreter _builder;
        private readonly ILogger _logger;

        public string TablePrefix { get; private set; }
        public ISqlDialect Dialect { get; private set; }
        public DbConnection Connection { get; private set; }
        public DbTransaction Transaction { get; private set; }
        public bool ThrowOnError { get; set; } = true;

        public SchemaBuilder(IConfiguration configuration, DbTransaction transaction, bool throwOnError = true)
        {
            Transaction = transaction;
            _logger = configuration.Logger;
            Connection = Transaction.Connection;
            _builder = CommandInterpreterFactory.For(Connection);
            Dialect = SqlDialectFactory.For(configuration.ConnectionFactory.DbConnectionType);
            TablePrefix = configuration.TablePrefix;
            ThrowOnError = throwOnError;
        }

        private void Execute(IEnumerable<string> statements)
        {
            foreach (var statement in statements)
            {
                _logger.LogTrace(statement);
                Connection.Execute(statement, null, Transaction);
            }
        }

        private string Prefix(string table)
        {
            return TablePrefix + table;
        }

        public ISchemaBuilder CreateMapIndexTable(string name, Action<ICreateTableCommand> table)
        {
            try
            {
                var collection = CollectionHelper.Current;
                var tableName = Prefix(collection.GetPrefixedName(name));
                var createTable = new CreateTableCommand(tableName);
                
                createTable
                    .Column<int>("Id", column => column.PrimaryKey().Identity().NotNull())
                    .Column<int>("DocumentId");

                table(createTable);
                Execute(_builder.CreateSql(createTable));

                CreateForeignKey("FK_" + tableName, name, new[] { "DocumentId" }, Store.DocumentTable, new[] { "Id" });
            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }

            return this;
        }

        public ISchemaBuilder CreateReduceIndexTable(string name, Action<ICreateTableCommand> table)
        {
            try
            {
                var collection = CollectionHelper.Current;
                var tableName = Prefix(collection.GetPrefixedName(name));
                var createTable = new CreateTableCommand(tableName);
                
                createTable
                    .Column<int>("Id", column => column.Identity().NotNull())
                    ;

                table(createTable);
                Execute(_builder.CreateSql(createTable));

                var bridgeTableName = name + "_" + Store.DocumentTable;
                var bridgeTableNameFK = "FK_" + Prefix(collection.GetPrefixedName(bridgeTableName));

                CreateTable(bridgeTableName, bridge => bridge
                    .Column<int>(name + "Id", column => column.NotNull())
                    .Column<int>("DocumentId", column => column.NotNull())
                );

                CreateForeignKey(bridgeTableNameFK + "_Id", bridgeTableName, new[] { name + "Id" }, name, new[] { "Id" });
                CreateForeignKey(bridgeTableNameFK + "_DocumentId", bridgeTableName, new[] { "DocumentId" }, Store.DocumentTable, new[] { "Id" });
            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }

            return this;
        }

        public ISchemaBuilder DropReduceIndexTable(string name)
        {
            try
            {
                var collection = CollectionHelper.Current;
                
                var bridgeTableName = name + "_" + Store.DocumentTable;
                var bridgeTableNameFK = "FK_" + Prefix(collection.GetPrefixedName(bridgeTableName));
                if (String.IsNullOrEmpty(Dialect.CascadeConstraintsString))
                {
                    DropForeignKey(bridgeTableName, bridgeTableNameFK + "_Id");
                    DropForeignKey(bridgeTableName, bridgeTableNameFK + "_DocumentId");
                }

                DropTable(bridgeTableName);
                DropTable(name);
            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }

            return this;
        }

        public ISchemaBuilder DropMapIndexTable(string name)
        {
            try
            {
                var collection = CollectionHelper.Current;
                var tableName = Prefix(collection.GetPrefixedName(name));
                if (String.IsNullOrEmpty(Dialect.CascadeConstraintsString))
                {
                    DropForeignKey(name, "FK_" + tableName);
                }

                DropTable(name);
            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }

            return this;
        }

        public ISchemaBuilder CreateTable(string name, Action<ICreateTableCommand> table)
        {
            try
            {
                var collection = CollectionHelper.Current;
                var tableName = Prefix(collection.GetPrefixedName(name));
                var createTable = new CreateTableCommand(tableName);
                table(createTable);
                Execute(_builder.CreateSql(createTable));
            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }

            return this;
        }

        public ISchemaBuilder AlterTable(string name, Action<IAlterTableCommand> table)
        {
            try
            {
                var collection = CollectionHelper.Current;
                var tableName = Prefix(collection.GetPrefixedName(name));
                var alterTable = new AlterTableCommand(tableName, Dialect, TablePrefix);
                table(alterTable);
                Execute(_builder.CreateSql(alterTable));
            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }

            return this;
        }

        public ISchemaBuilder DropTable(string name)
        {
            try
            {
                var collection = CollectionHelper.Current;
                var tableName = Prefix(collection.GetPrefixedName(name));
                var deleteTable = new DropTableCommand(tableName);
                Execute(_builder.CreateSql(deleteTable));
            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }

            return this;
        }

        public ISchemaBuilder CreateForeignKey(string name, string srcTable, string[] srcColumns, string destTable, string[] destColumns)
        {
            try
            {
                var collection = CollectionHelper.Current;
                var srcTableName = Prefix(collection.GetPrefixedName(srcTable));
                var destTableName = Prefix(collection.GetPrefixedName(destTable));
                var command = new CreateForeignKeyCommand(name, srcTableName, srcColumns, destTableName, destColumns);
                Execute(_builder.CreateSql(command));
            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }

            return this;
        }

        public ISchemaBuilder CreateForeignKey(string name, string srcModule, string srcTable, string[] srcColumns, string destTable, string[] destColumns)
        {
            try
            {
                var collection = CollectionHelper.Current;
                var srcTableName = Prefix(collection.GetPrefixedName(srcTable));
                var destTableName = Prefix(collection.GetPrefixedName(destTable));
                var command = new CreateForeignKeyCommand(name, srcTableName, srcColumns, destTableName, destColumns);
                Execute(_builder.CreateSql(command));
            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }
            return this;
        }

        public ISchemaBuilder CreateForeignKey(string name, string srcTable, string[] srcColumns, string destModule, string destTable, string[] destColumns)
        {
            try
            {
                var collection = CollectionHelper.Current;
                var srcTableName = Prefix(collection.GetPrefixedName(srcTable));
                var destTableName = Prefix(collection.GetPrefixedName(destTable));
                var command = new CreateForeignKeyCommand(name, srcTableName, srcColumns, destTableName, destColumns);
                Execute(_builder.CreateSql(command));

            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }

            return this;
        }

        public ISchemaBuilder CreateForeignKey(string name, string srcModule, string srcTable, string[] srcColumns, string destModule, string destTable, string[] destColumns)
        {
            try
            {
                var collection = CollectionHelper.Current;
                var srcTableName = Prefix(collection.GetPrefixedName(srcTable));
                var destTableName = Prefix(collection.GetPrefixedName(destTable));
                var command = new CreateForeignKeyCommand(name, srcTableName, srcColumns, destTableName, destColumns);
                Execute(_builder.CreateSql(command));
            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }

            return this;
        }

        public ISchemaBuilder DropForeignKey(string srcTable, string name)
        {
            try
            {
                var collection = CollectionHelper.Current;
                var srcTableName = Prefix(collection.GetPrefixedName(srcTable));
                var command = new DropForeignKeyCommand(srcTableName, name);
                Execute(_builder.CreateSql(command));
            }
            catch
            {
                if (ThrowOnError)
                {
                    throw;
                }
            }

            return this;
        }
    }
}
