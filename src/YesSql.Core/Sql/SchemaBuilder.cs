using System;
using System.Data;
using Dapper;
using System.Collections.Generic;
using YesSql.Core.Sql.Schema;
using System.Data.Common;

namespace YesSql.Core.Sql
{
    public class SchemaBuilder
    {
        private ISchemaBuilder _builder;
        private DbConnection _connection;
        private DbTransaction _transaction;

        public SchemaBuilder(DbConnection connection, DbTransaction transaction)
        {
            _builder = SchemaBuilderFactory.For(connection);
            _connection = connection;
            _transaction = transaction;
        }

        private void Execute(IEnumerable<string> statements)
        {
            foreach(var statement in statements)
            {
                _connection.Execute(statement, null, _transaction);
            }
        }

        public SchemaBuilder CreateMapIndexTable(string name, Action<CreateTableCommand> table)
        {
            var createTable = new CreateTableCommand(name);

            createTable
                .Column<int>("Id", column => column.PrimaryKey().Identity().NotNull())
                .Column<int>("DocumentId");

            table(createTable);
            Execute(_builder.CreateSql(createTable));

            CreateForeignKey("FK_" + name, name, new[] { "DocumentId" }, "Document", new[] { "Id" });
            return this;
        }

        public SchemaBuilder CreateReduceIndexTable(string name, Action<CreateTableCommand> table)
        {
            var createTable = new CreateTableCommand(name);

            createTable
                .Column<int>("Id", column => column.Identity().NotNull())
                ;

            table(createTable);
            Execute(_builder.CreateSql(createTable));

            var bridgeTableName = name + "_Document";

            CreateTable(bridgeTableName, bridge => bridge
                .Column<int>(name + "Id", column => column.NotNull())
                .Column<int>("DocumentId", column => column.NotNull())
            );

            CreateForeignKey("FK_" + bridgeTableName + "_Id", bridgeTableName, new[] { name + "Id" }, name, new[] { "Id" });
            CreateForeignKey("FK_" + bridgeTableName + "DocumentId" , bridgeTableName, new[] { "DocumentId" }, "Document", new[] { "Id" });
            return this;
        }

        public SchemaBuilder CreateTable(string name, Action<CreateTableCommand> table)
        {
            var createTable = new CreateTableCommand(name);
            table(createTable);
            Execute(_builder.CreateSql(createTable));
            return this;
        }

        public SchemaBuilder AlterTable(string name, Action<AlterTableCommand> table)
        {
            var alterTable = new AlterTableCommand(name);
            table(alterTable);
            Execute(_builder.CreateSql(alterTable));
            return this;
        }

        public SchemaBuilder DropTable(string name)
        {
            var deleteTable = new DropTableCommand(name);
            Execute(_builder.CreateSql(deleteTable));
            return this;
        }

        public SchemaBuilder CreateForeignKey(string name, string srcTable, string[] srcColumns, string destTable, string[] destColumns)
        {
            var command = new CreateForeignKeyCommand(name, srcTable, srcColumns, destTable, destColumns);
            Execute(_builder.CreateSql(command));
            return this;
        }

        public SchemaBuilder CreateForeignKey(string name, string srcModule, string srcTable, string[] srcColumns, string destTable, string[] destColumns)
        {
            var command = new CreateForeignKeyCommand(name, srcTable, srcColumns, destTable, destColumns);
            Execute(_builder.CreateSql(command));
            return this;
        }

        public SchemaBuilder CreateForeignKey(string name, string srcTable, string[] srcColumns, string destModule, string destTable, string[] destColumns)
        {
            var command = new CreateForeignKeyCommand(name, srcTable, srcColumns, destTable, destColumns);
            Execute(_builder.CreateSql(command));
            return this;
        }

        public SchemaBuilder CreateForeignKey(string name, string srcModule, string srcTable, string[] srcColumns, string destModule, string destTable, string[] destColumns)
        {
            var command = new CreateForeignKeyCommand(name, srcTable, srcColumns, destTable, destColumns);
            Execute(_builder.CreateSql(command));
            return this;
        }

        public SchemaBuilder DropForeignKey(string srcTable, string name)
        {
            var command = new DropForeignKeyCommand(srcTable, name);
            Execute(_builder.CreateSql(command));
            return this;
        }

        public SchemaBuilder DropForeignKey(string srcModule, string srcTable, string name)
        {
            var command = new DropForeignKeyCommand(srcTable, name);
            Execute(_builder.CreateSql(command));
            return this;
        }

    }
}
