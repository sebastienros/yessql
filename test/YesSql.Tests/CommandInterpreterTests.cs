using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using YesSql.Provider.MySql;
using YesSql.Provider.PostgreSql;
using YesSql.Provider.Sqlite;
using YesSql.Provider.SqlServer;
using YesSql.Sql;
using YesSql.Sql.Schema;

namespace YesSql.Tests
{
    public class CommandInterpreterTests
    {
        private static (SqliteCommandInterpreter Interpreter, SqliteDialect Dialect) CreateInterpreter()
        {
            var dialect = new SqliteDialect();
            var configuration = new Configuration
            {
                SqlDialect = dialect
            };

            return (new SqliteCommandInterpreter(configuration), dialect);
        }

        [Fact]
        public void AlterTableShouldDropIndexBeforeAddingIndex()
        {
            var (interpreter, dialect) = CreateInterpreter();

            var alter = new AlterTableCommand("People", dialect, "tp_");

            // Order of registration intentionally puts the add before the drop to
            // verify the interpreter reorders them.
            alter.CreateIndex("IDX_New", "Name");
            alter.DropIndex("IDX_Old");

            var statements = interpreter.Run(alter).ToList();

            var dropIndex = statements.FindIndex(s => s.Contains("drop index"));
            var createIndex = statements.FindIndex(s => s.Contains("create index"));

            Assert.True(dropIndex >= 0, "Expected a drop index statement.");
            Assert.True(createIndex >= 0, "Expected a create index statement.");
            Assert.True(dropIndex < createIndex, "Drop index must be emitted before create index.");
        }

        [Fact]
        public void AlterTableShouldDropIndexBeforeDroppingColumns()
        {
            var (interpreter, dialect) = CreateInterpreter();

            var alter = new AlterTableCommand("People", dialect, "tp_");

            alter.DropColumn("Name");
            alter.DropIndex("IDX_Name");

            var statements = interpreter.Run(alter).ToList();

            var dropIndex = statements.FindIndex(s => s.Contains("drop index"));
            var dropColumn = statements.FindIndex(s => s.Contains("drop column"));

            Assert.True(dropIndex >= 0, "Expected a drop index statement.");
            Assert.True(dropColumn >= 0, "Expected a drop column statement.");
            Assert.True(dropIndex < dropColumn, "Drop index must be emitted before drop column.");
        }
    
        private static string CreateIndexSql(ISqlDialect dialect, Func<IConfiguration, ICommandInterpreter> interpreter, bool concurrent)
        {
            var configuration = new Configuration { SqlDialect = dialect };

            var alter = new AlterTableCommand("People", dialect, "", concurrent);
            alter.CreateIndex("IDX_Name", "Name", "Age");

            return Assert.Single(interpreter(configuration).CreateSql([alter])).Trim();
        }

        [Fact]
        public void AlterTableShouldNotCreateIndexConcurrentlyByDefault()
        {
            var alter = new AlterTableCommand("People", new PostgreSqlDialect(), "");
            alter.CreateIndex("IDX_Name", "Name");

            Assert.False(Assert.IsType<AddIndexCommand>(Assert.Single(alter.TableCommands)).Concurrent);
        }

        [Fact]
        public void PostgreSqlShouldCreateIndexConcurrently()
        {
            var dialect = new PostgreSqlDialect();

            Assert.Equal("""create index concurrently "IDX_Name" on "People" ("Name", "Age")""", CreateIndexSql(dialect, c => new PostgreSqlCommandInterpreter(c), true));
            Assert.Equal("""create index "IDX_Name" on "People" ("Name", "Age")""", CreateIndexSql(dialect, c => new PostgreSqlCommandInterpreter(c), false));
        }

        [Fact]
        public void SqlServerShouldCreateIndexOnlineOnSupportedEditions()
        {
            var dialect = new SqlServerDialect();

            Assert.Equal(
                "if serverproperty('EngineEdition') in (3, 5, 8) exec(N'create index [IDX_Name] on [People] ([Name], [Age]) with (online = on)') " +
                "else create index [IDX_Name] on [People] ([Name], [Age])",
                CreateIndexSql(dialect, c => new SqlServerCommandInterpreter(c), true));
            Assert.Equal("create index [IDX_Name] on [People] ([Name], [Age])", CreateIndexSql(dialect, c => new SqlServerCommandInterpreter(c), false));
        }

        [Fact]
        public void MySqlShouldCreateIndexWithoutLock()
        {
            var dialect = new MySqlDialect();

            Assert.Equal("create index `IDX_Name` on `People` (`Name`, `Age`) algorithm=inplace lock=none", CreateIndexSql(dialect, c => new MySqlCommandInterpreter(c), true));
            Assert.Equal("create index `IDX_Name` on `People` (`Name`, `Age`)", CreateIndexSql(dialect, c => new MySqlCommandInterpreter(c), false));
        }

        [Fact]
        public void SqliteShouldIgnoreConcurrentIndexCreation()
        {
            var dialect = new SqliteDialect();

            Assert.False(dialect.SupportsConcurrentIndexCreation);
            Assert.Equal("create index [IDX_Name] on [People] ([Name], [Age])", CreateIndexSql(dialect, c => new SqliteCommandInterpreter(c), true));
        }

        [Fact]
        public void ConcurrentIndexCreationShouldBeEnabledByDefault()
        {
            var configuration = new Configuration();

            Assert.True(configuration.ConcurrentIndexCreationEnabled);
            Assert.False(configuration.DisableConcurrentIndexCreation().ConcurrentIndexCreationEnabled);
        }
    }
}
