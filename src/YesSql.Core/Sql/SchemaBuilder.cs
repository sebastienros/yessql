using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using YesSql.Collections;
using YesSql.Naming;
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
        public NamingCaseProvider NamingCaseProvider;
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
            NamingCaseProvider = new NamingCaseProvider(configuration.NamingCase);
        }

        private string N(string input) => NamingCaseProvider.GetName(input);

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
                var createTable = new CreateTableCommand(Prefix(N(name)));
                var collection = CollectionHelper.Current;
                var documentTable = collection.GetPrefixedName(N(Store.DocumentTable));

                createTable
                    .Column<int>(N("Id"), column => column.PrimaryKey().Identity().NotNull())
                    .Column<int>(N("DocumentId"));

                table(createTable);
                Execute(_builder.CreateSql(createTable));

                CreateForeignKey("FK_" + N(name), N(name), new[] { N("DocumentId") }, documentTable, new[] { N("Id") });
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
                var createTable = new CreateTableCommand(Prefix(N(name)));
                var collection = CollectionHelper.Current;
                var documentTable = collection.GetPrefixedName(N(Store.DocumentTable));

                createTable
                    .Column<int>(N("Id"), column => column.Identity().NotNull())
                    ;

                table(createTable);
                Execute(_builder.CreateSql(createTable));

                var bridgeTableName = N(name) + "_" + documentTable;

                CreateTable(bridgeTableName, bridge => bridge
                    .Column<int>(N(name + "Id"), column => column.NotNull())
                    .Column<int>(N("DocumentId"), column => column.NotNull())
                );

                CreateForeignKey("FK_" + bridgeTableName + "_" + N("Id"), bridgeTableName, new[] { N(name + "Id") }, N(name), new[] { N("Id") });
                CreateForeignKey("FK_" + bridgeTableName + "_" + N("DocumentId"), bridgeTableName, new[] { N("DocumentId") }, documentTable, new[] { N("Id") });
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
                var documentTable = collection.GetPrefixedName(N(Store.DocumentTable));

                var bridgeTableName = N(name) + "_" + documentTable;

                if (String.IsNullOrEmpty(Dialect.CascadeConstraintsString))
                {
                    DropForeignKey(bridgeTableName, "FK_" + bridgeTableName + "_" + N("Id"));
                    DropForeignKey(bridgeTableName, "FK_" + bridgeTableName + "_" + N("DocumentId"));
                }

                DropTable(bridgeTableName);
                DropTable(N(name));
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
                if (String.IsNullOrEmpty(Dialect.CascadeConstraintsString))
                {
                    DropForeignKey(N(name), "FK_" + N(name));
                }

                DropTable(N(name));
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
                var createTable = new CreateTableCommand(Prefix(N(name)));
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
                var alterTable = new AlterTableCommand(Prefix(N(name)), Dialect, TablePrefix);
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
                var deleteTable = new DropTableCommand(Prefix(N(name)));
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
                var command = new CreateForeignKeyCommand(Prefix(name), Prefix(N(srcTable)), srcColumns, Prefix(N(destTable)), destColumns);
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
                var command = new CreateForeignKeyCommand(Prefix(name), Prefix(N(srcTable)), srcColumns, Prefix(N(destTable)), destColumns);
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
                var command = new CreateForeignKeyCommand(Prefix(name), Prefix(N(srcTable)), srcColumns, Prefix(N(destTable)), destColumns);
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
                var command = new CreateForeignKeyCommand(Prefix(name), Prefix(N(srcTable)), srcColumns, Prefix(N(destTable)), destColumns);
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
                var command = new DropForeignKeyCommand(Prefix(N(srcTable)), Prefix(name));
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
