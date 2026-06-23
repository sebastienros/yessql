using System.Collections.Generic;
using System.Linq;
using Xunit;
using YesSql.Provider.Sqlite;
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
    }
}
