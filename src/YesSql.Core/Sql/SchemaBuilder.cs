using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql.Diagnostics;
using YesSql.Sql.Schema;

namespace YesSql.Sql
{
    public class SchemaBuilder : ISchemaBuilder
    {
        private readonly ICommandInterpreter _commandInterpreter;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _concurrentIndexCreation;

        public string TablePrefix { get; private set; }
        public ISqlDialect Dialect { get; private set; }
        public ITableNameConvention TableNameConvention { get; private set; }
        public DbConnection Connection { get; private set; }
        public DbTransaction Transaction { get; private set; }
        public bool ThrowOnError { get; private set; }
        public IdentityColumnSize IdentityColumnSize { get; set; }

        public SchemaBuilder(IConfiguration configuration, DbTransaction transaction, bool throwOnError = true)
            : this(configuration, transaction.Connection, transaction, throwOnError)
        {
        }

        /// <summary>
        /// Creates a schema builder that runs each statement on its own, outside of any transaction.
        /// </summary>
        /// <remarks>
        /// Some databases can only create indexes without blocking writes outside of a transaction, like PostgreSQL with
        /// <c>CREATE INDEX CONCURRENTLY</c>. A failing statement doesn't revert the previous ones.
        /// </remarks>
        public SchemaBuilder(IConfiguration configuration, DbConnection connection, bool throwOnError = true)
            : this(configuration, connection, null, throwOnError)
        {
        }

        private SchemaBuilder(IConfiguration configuration, DbConnection connection, DbTransaction transaction, bool throwOnError)
        {
            ArgumentNullException.ThrowIfNull(connection);

            Transaction = transaction;
            _configuration = configuration;
            _logger = configuration.Logger;
            Connection = connection;
            _commandInterpreter = configuration.CommandInterpreter;
            Dialect = configuration.SqlDialect;
            TablePrefix = configuration.TablePrefix;
            ThrowOnError = throwOnError;
            TableNameConvention = configuration.TableNameConvention;
            IdentityColumnSize = configuration.IdentityColumnSize;

            _concurrentIndexCreation = configuration.ConcurrentIndexCreationEnabled
                && Dialect.SupportsConcurrentIndexCreation
                && (transaction == null || Dialect.SupportsConcurrentIndexCreationInTransaction);
        }

        public async Task CreateMapIndexTableAsync(Type indexType, Action<ICreateTableCommand> table, string collection)
        {
            try
            {
                var indexName = indexType.Name;
                var indexTable = TableNameConvention.GetIndexTable(indexType, collection);
                var createTable = new CreateTableCommand(Prefix(indexTable));
                var documentTable = TableNameConvention.GetDocumentTable(collection);

                // NB: Identity() implies PrimaryKey()

                createTable
                    .Column(IdentityColumnSize, "Id", column => column.Identity().NotNull())
                    .Column(IdentityColumnSize, "DocumentId")
                    ;

                table(createTable);
                await ExecuteAsync(_commandInterpreter.CreateSql(createTable), indexTable);

                await CreateForeignKeyAsync("FK_" + (collection ?? "") + indexName, indexTable, new[] { "DocumentId" }, documentTable, new[] { "Id" });

                // NB: Don't index the custom columns here. Those indexes belong to the application that defines the columns,
                // as YesSql doesn't know its filters. Leading with the filter columns and ending with DocumentId works best for counts.
                // Leading with DocumentId can be worse than no index: on MySQL, count(distinct DocumentId) filtered on another column
                // then skip scans the whole index.

                await AlterTableAsync(indexTable, table =>
                    table.CreateIndex($"IDX_FK_{indexTable}", "DocumentId"),
                    concurrentIndexCreation: false
                    );
            }
            catch (Exception e)
            {
                if (ThrowOnError || e is not DbException)
                {
                    throw;
                }
            }
        }

        public async Task CreateReduceIndexTableAsync(Type indexType, Action<ICreateTableCommand> table, string collection = null)
        {
            try
            {
                var indexName = indexType.Name;
                var indexTable = TableNameConvention.GetIndexTable(indexType, collection);
                var createTable = new CreateTableCommand(Prefix(indexTable));
                var documentTable = TableNameConvention.GetDocumentTable(collection);

                // NB: Identity() implies PrimaryKey()

                createTable.Column(IdentityColumnSize, "Id", column => column.Identity().NotNull())
                    ;

                table(createTable);
                await ExecuteAsync(_commandInterpreter.CreateSql(createTable), indexTable);

                var bridgeTableName = indexTable + "_" + documentTable;

                await CreateTableAsync(bridgeTableName, bridge => bridge
                    .Column(IdentityColumnSize, indexName + "Id", column => column.NotNull())
                    .Column(IdentityColumnSize, "DocumentId", column => column.NotNull())
                );

                await CreateForeignKeyAsync("FK_" + bridgeTableName + "_Id", bridgeTableName, new[] { indexName + "Id" }, indexTable, new[] { "Id" });
                await CreateForeignKeyAsync("FK_" + bridgeTableName + "_DocumentId", bridgeTableName, new[] { "DocumentId" }, documentTable, new[] { "Id" });

                await AlterTableAsync(bridgeTableName, table =>
                    table.CreateIndex($"IDX_FK_{bridgeTableName}", indexName + "Id", "DocumentId"),
                    concurrentIndexCreation: false
                    );

                // NB: The index above can't serve the foreign key on DocumentId, which every document delete has to check.
                // MySQL creates an index for that foreign key on its own and replaces it with this one.
                // Keep it on DocumentId alone, adding the reduce index id gave counts no real benefit.

                await AlterTableAsync(bridgeTableName, table =>
                    table.CreateIndex($"IDX_FK_{bridgeTableName}_DocumentId", "DocumentId"),
                    concurrentIndexCreation: false
                    );
            }
            catch (Exception e)
            {
                if (ThrowOnError || e is not DbException)
                {
                    throw;
                }
            }
        }

        public async Task DropReduceIndexTableAsync(Type indexType, string collection = null)
        {
            try
            {
                var indexTable = TableNameConvention.GetIndexTable(indexType, collection);
                var documentTable = TableNameConvention.GetDocumentTable(collection);

                var bridgeTableName = indexTable + "_" + documentTable;

                if (string.IsNullOrEmpty(Dialect.CascadeConstraintsString))
                {
                    await DropForeignKeyAsync(bridgeTableName, "FK_" + bridgeTableName + "_Id");
                    await DropForeignKeyAsync(bridgeTableName, "FK_" + bridgeTableName + "_DocumentId");
                }

                await DropTableAsync(bridgeTableName);
                await DropTableAsync(indexTable);
            }
            catch (Exception e)
            {
                if (ThrowOnError || e is not DbException)
                {
                    throw;
                }
            }
        }

        public async Task DropMapIndexTableAsync(Type indexType, string collection = null)
        {
            try
            {
                var indexName = indexType.Name;
                var indexTable = TableNameConvention.GetIndexTable(indexType, collection);

                if (string.IsNullOrEmpty(Dialect.CascadeConstraintsString))
                {
                    await DropForeignKeyAsync(indexTable, "FK_" + (collection ?? string.Empty) + indexName);
                }

                await DropTableAsync(indexTable);
            }
            catch (Exception e)
            {
                if (ThrowOnError || e is not DbException)
                {
                    throw;
                }
            }
        }

        public async Task CreateTableAsync(string name, Action<ICreateTableCommand> table)
        {
            try
            {
                var createTable = new CreateTableCommand(Prefix(name));
                table(createTable);
                await ExecuteAsync(_commandInterpreter.CreateSql(createTable), name);
            }
            catch (Exception e)
            {
                if (ThrowOnError || e is not DbException)
                {
                    throw;
                }
            }
        }

        public Task AlterTableAsync(string name, Action<IAlterTableCommand> table)
            => AlterTableAsync(name, table, _concurrentIndexCreation);

        // The indexes of a table created by the same call don't need to be created concurrently, the table is empty.
        private async Task AlterTableAsync(string name, Action<IAlterTableCommand> table, bool concurrentIndexCreation)
        {
            try
            {
                var alterTable = new AlterTableCommand(Prefix(name), Dialect, TablePrefix, concurrentIndexCreation);
                table(alterTable);
                await ExecuteAsync(_commandInterpreter.CreateSql(alterTable), name);
            }
            catch (Exception e)
            {
                if (ThrowOnError || e is not DbException)
                {
                    throw;
                }
            }
        }

        public async Task AlterIndexTableAsync(Type indexType, Action<IAlterTableCommand> table, string collection)
        {
            var indexTable = TableNameConvention.GetIndexTable(indexType, collection);
            await AlterTableAsync(indexTable, table);
        }

        public async Task DropTableAsync(string name)
        {
            try
            {
                var deleteTable = new DropTableCommand(Prefix(name));
                await ExecuteAsync(_commandInterpreter.CreateSql(deleteTable), name);
            }
            catch (Exception e)
            {
                if (ThrowOnError || e is not DbException)
                {
                    throw;
                }
            }
        }

        public async Task CreateForeignKeyAsync(string name, string srcTable, string[] srcColumns, string destTable, string[] destColumns)
        {
            try
            {
                var command = new CreateForeignKeyCommand(Dialect.FormatKeyName(Prefix(name)), Prefix(srcTable), srcColumns, Prefix(destTable), destColumns);
                var sql = _commandInterpreter.CreateSql(command);
                await ExecuteAsync(sql, srcTable);
            }
            catch (Exception e)
            {
                if (ThrowOnError || e is not DbException)
                {
                    throw;
                }
            }
        }

        public async Task DropForeignKeyAsync(string srcTable, string name)
        {
            try
            {
                var command = new DropForeignKeyCommand(Dialect.FormatKeyName(Prefix(srcTable)), Prefix(name));
                await ExecuteAsync(_commandInterpreter.CreateSql(command), srcTable);
            }
            catch (Exception e)
            {
                if (ThrowOnError || e is not DbException)
                {
                    throw;
                }
            }
        }

        public async Task CreateSchemaAsync(string schema)
        {
            try
            {
                var createSchema = new CreateSchemaCommand(schema);
                await ExecuteAsync(_commandInterpreter.CreateSql(createSchema), null);
            }
            catch (Exception e)
            {
                if (ThrowOnError || e is not DbException)
                {
                    throw;
                }
            }
        }

        public ISchemaBuilder AlterTable(string name, Action<IAlterTableCommand> table)
        {
            AlterTableAsync(name, table).GetAwaiter().GetResult();
            return this;
        }

        public ISchemaBuilder AlterIndexTable(Type indexType, Action<IAlterTableCommand> table, string collection)
        {
            AlterIndexTableAsync(indexType, table, collection).GetAwaiter().GetResult();
            return this;
        }

        public ISchemaBuilder CreateForeignKey(string name, string srcTable, string[] srcColumns, string destTable, string[] destColumns)
        {
            CreateForeignKeyAsync(name, srcTable, srcColumns, destTable, destColumns).GetAwaiter().GetResult();
            return this;
        }

        public ISchemaBuilder CreateMapIndexTable(Type indexType, Action<ICreateTableCommand> table, string collection)
        {
            CreateMapIndexTableAsync(indexType, table, collection).GetAwaiter().GetResult();
            return this;
        }

        public ISchemaBuilder CreateReduceIndexTable(Type indexType, Action<ICreateTableCommand> table, string collection)
        {
            CreateReduceIndexTableAsync(indexType, table, collection).GetAwaiter().GetResult();
            return this;
        }

        public ISchemaBuilder CreateTable(string name, Action<ICreateTableCommand> table)
        {
            CreateTableAsync(name, table).GetAwaiter().GetResult();
            return this;
        }

        public ISchemaBuilder DropForeignKey(string srcTable, string name)
        {
            DropForeignKeyAsync(srcTable, name).GetAwaiter().GetResult();
            return this;
        }

        public ISchemaBuilder DropMapIndexTable(Type indexType, string collection = null)
        {
            DropMapIndexTableAsync(indexType, collection).GetAwaiter().GetResult();
            return this;
        }

        public ISchemaBuilder DropReduceIndexTable(Type indexType, string collection = null)
        {
            DropReduceIndexTableAsync(indexType, collection).GetAwaiter().GetResult();
            return this;
        }

        public ISchemaBuilder DropTable(string name)
        {
            DropTableAsync(name).GetAwaiter().GetResult();
            return this;
        }

        public ISchemaBuilder CreateSchema(string schema)
        {
            CreateSchemaAsync(schema).GetAwaiter().GetResult();
            return this;
        }

        private async Task ExecuteAsync(IEnumerable<string> statements, string table)
        {
            foreach (var statement in statements)
            {
                if (string.IsNullOrEmpty(statement))
                {
                    continue;
                }

                _logger.LogTrace(statement);

                var operationName = YesSqlInstrumentation.IsEnabled ? YesSqlInstrumentation.GetStatementOperationName(statement) : null;

                await YesSqlInstrumentation.ExecuteAsync(
                    new DbOperation(_configuration, Connection, operationName, table, statement),
                    new CommandDefinition(statement, null, Transaction));
            }
        }
        private string Prefix(string table)
            => TablePrefix + table;
    }
}
