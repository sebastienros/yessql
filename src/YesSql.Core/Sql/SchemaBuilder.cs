using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using YesSql.Core.Collections;
using YesSql.Core.Services;
using YesSql.Core.Sql.Schema;

namespace YesSql.Core.Sql
{
    public class SchemaBuilder
    {
        private ICommandInterpreter _builder;
        private string _tablePrefix;
        public DbConnection Connection { get; private set; }
        public DbTransaction Transaction { get; private set; }

        public SchemaBuilder(DbConnection connection, DbTransaction transaction, string tablePrefix)
        {
            _builder = SchemaBuilderFactory.For(connection);
            _tablePrefix = tablePrefix;
            Connection = connection;
            Transaction = transaction;
        }

        private void Execute(IEnumerable<string> statements)
        {
            foreach (var statement in statements)
            {
                Connection.Execute(statement, null, Transaction);
            }
        }

        private string FormatTable(string table)
        {
            return _tablePrefix + table;
        }

        public SchemaBuilder CreateMapIndexTable(string name, Action<CreateTableCommand> table)
        {
            var createTable = new CreateTableCommand(FormatTable(name));
            var collection = CollectionHelper.Current;
            var documentTable = collection.GetPrefixedName(Store.DocumentTable);

            createTable
                .Column<int>("Id", column => column.PrimaryKey().Identity().NotNull())
                .Column<int>("DocumentId");

            table(createTable);
            Execute(_builder.CreateSql(createTable));

            CreateForeignKey("FK_" + name, name, new[] { "DocumentId" }, documentTable, new[] { "Id" });
            return this;
        }

        public SchemaBuilder CreateReduceIndexTable(string name, Action<CreateTableCommand> table)
        {
            var createTable = new CreateTableCommand(FormatTable(name));
            var collection = CollectionHelper.Current;
            var documentTable = collection.GetPrefixedName(Store.DocumentTable);

            createTable
                .Column<int>("Id", column => column.Identity().NotNull())
                ;

            table(createTable);
            Execute(_builder.CreateSql(createTable));

            var bridgeTableName = name + "_" + documentTable;

            CreateTable(bridgeTableName, bridge => bridge
                .Column<int>(name + "Id", column => column.NotNull())
                .Column<int>("DocumentId", column => column.NotNull())
            );

            CreateForeignKey("FK_" + bridgeTableName + "_Id", bridgeTableName, new[] { name + "Id" }, name, new[] { "Id" });
            CreateForeignKey("FK_" + bridgeTableName + "_DocumentId", bridgeTableName, new[] { "DocumentId" }, documentTable, new[] { "Id" });
            return this;
        }

        public SchemaBuilder DropReduceIndexTable(string name)
        {
            var collection = CollectionHelper.Current;
            var documentTable = collection.GetPrefixedName(Store.DocumentTable);

            var bridgeTableName = name + "_" + documentTable;

            DropForeignKey(bridgeTableName, "FK_" + bridgeTableName + "_Id");
            DropForeignKey(bridgeTableName, "FK_" + bridgeTableName + "_DocumentId");

            DropTable(bridgeTableName);
            DropTable(name);

            return this;
        }

        public SchemaBuilder DropMapIndexTable(string name)
        {
            DropForeignKey(name, "FK_" + name);
            DropTable(name);

            return this;
        }

        public SchemaBuilder CreateTable(string name, Action<CreateTableCommand> table)
        {
            var createTable = new CreateTableCommand(FormatTable(name));
            table(createTable);
            Execute(_builder.CreateSql(createTable));
            return this;
        }

        public SchemaBuilder AlterTable(string name, Action<AlterTableCommand> table)
        {
            var alterTable = new AlterTableCommand(FormatTable(name));
            table(alterTable);
            Execute(_builder.CreateSql(alterTable));
            return this;
        }

        public SchemaBuilder DropTable(string name)
        {
            try
            {
                var deleteTable = new DropTableCommand(FormatTable(name));
                Execute(_builder.CreateSql(deleteTable));
            }
            catch
            {
            }

            return this;
        }

        public SchemaBuilder CreateForeignKey(string name, string srcTable, string[] srcColumns, string destTable, string[] destColumns)
        {
            var command = new CreateForeignKeyCommand(FormatTable(name), FormatTable(srcTable), srcColumns, FormatTable(destTable), destColumns);
            Execute(_builder.CreateSql(command));
            return this;
        }

        public SchemaBuilder CreateForeignKey(string name, string srcModule, string srcTable, string[] srcColumns, string destTable, string[] destColumns)
        {
            var command = new CreateForeignKeyCommand(FormatTable(name), FormatTable(srcTable), srcColumns, FormatTable(destTable), destColumns);
            Execute(_builder.CreateSql(command));
            return this;
        }

        public SchemaBuilder CreateForeignKey(string name, string srcTable, string[] srcColumns, string destModule, string destTable, string[] destColumns)
        {
            var command = new CreateForeignKeyCommand(FormatTable(name), FormatTable(srcTable), srcColumns, FormatTable(destTable), destColumns);
            Execute(_builder.CreateSql(command));
            return this;
        }

        public SchemaBuilder CreateForeignKey(string name, string srcModule, string srcTable, string[] srcColumns, string destModule, string destTable, string[] destColumns)
        {
            var command = new CreateForeignKeyCommand(FormatTable(name), FormatTable(srcTable), srcColumns, FormatTable(destTable), destColumns);
            Execute(_builder.CreateSql(command));
            return this;
        }

        public SchemaBuilder DropForeignKey(string srcTable, string name)
        {
            try
            {
                var command = new DropForeignKeyCommand(FormatTable(srcTable), name);
                Execute(_builder.CreateSql(command));
            }
            catch
            {
            }

            return this;
        }
    }
}
